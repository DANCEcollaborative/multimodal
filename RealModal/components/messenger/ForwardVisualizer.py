from components.messenger.BaseMessenger import ImageMessenger
from common.GlobalVariables import GV

import _thread
import cv2
import socket
import threading

from common.logprint import get_logger
from Socket.Client import DataTransmissionClient as DTC

import time

logger = get_logger(__name__)

@GV.register_messenger("forward_visualizer")
class ForwardVisualizer(ImageMessenger):
    """
        This class is designed for processing image data in real-time. Generally it has the following few functions:
        1. Receive image data from ActiveMQ (derived from ImageListener).
        2. Forward image data to the server.
        3. Manage image processing components.
        4. Visualize image data for selected cameras.

        TODO:
        In the future, if someone interested in software engineer sees this comment, think about whether it's
        possible to split the forwarding and visualizing part to different messengers. (I personally believe it's
        possible)
    """

    # addr_in: tuple, addr_out: tuple, topic = None
    def __init__(self, config):
        """
        :param config:
            Configuration Dictionary.
            Address to and from the server must be provided. There are two ways to do so:
            (1) Provide domains named addr_in and addr_out. The format should be:
                (ip, port), eg: (127.0.0.1, 2333)
            (2) Provide domains named server_ip and port. The port should be a dict where both "upstream" and
                "downstream" are included. Example is included in config/config.yaml
            Listener should also be provided within the config dictionary.
        """
        if "decode" not in config:
            config.decode = False
        super(ForwardVisualizer, self).__init__(config)
        addr_in, addr_out = self.build_addr_from_config()
        self.running = False
        self.image_socket = DTC(addr_out)
        self.image_socket.set_timeout(5)
        self.receive_socket = DTC(addr_in)
        self.receive_socket.set_timeout(5)
        self.send_lock = threading.Lock()
        self.register_id = []
        self.register_topic = dict()
        self.register = dict()
        self.register_buffer = dict()
        self.register_buffer_local = dict()
        self.register_lock = dict()
        self.raw_image_buffer = dict()
        self.raw_image_lock = threading.Lock()
        self.base64_image_buffer = dict()
        self.base64_image_lock = threading.Lock()
        self.ready_to_send = False
        self.attach_listeners()

        self.num_recv_from_PSI = 0
        self.num_send_to_server = 0
        self.num_fail_send_to_server = 0
        self.num_recv_from_server = 0
        self.num_restart_socket = 0

    def build_addr_from_config(self, config=None):
        if config is None:
            config = self.config
        addr_in = (config.address.ip, config.address.port.downstream)
        addr_out = (config.address.ip, config.address.port.upstream)
        return addr_in, addr_out

    def attach_listeners(self, config=None):
        if config is None:
            config = self.config
        for listener_name in config.listeners:
            listener_config = config.listeners[listener_name]
            self.add(listener_name, listener_config)

    def process_property(self, prop_str: str):
        flag = super().process_property(prop_str)
        if not flag:
            if prop_str == "END":
                self.ready_to_send = True

    def add(self, listener_name: str, config):
        listener_cls = GV.get_listener_class(listener_name)
        if listener_cls is None:
            logger.warning(f"Can't find listener {listener_name}")
            return
        listener = listener_cls(config)
        topic = listener.topic
        lis_id = id(listener)
        if topic in self.register_topic:
            logger.info("Change listener for topic %s" % topic)
            for i, old_id in enumerate(self.register_id):
                if old_id == self.register_topic[topic]:
                    self.register_id[i] = lis_id
                    del self.register_lock[old_id]
                    del self.register_buffer[old_id]
                    del self.register_buffer_local[old_id]
                    del self.register[old_id]
                    break
        else:
            self.register_id.append(lis_id)
        self.register_topic[topic] = lis_id
        self.register_lock[lis_id] = threading.Lock()
        self.register_buffer[lis_id] = dict()
        self.register_buffer_local[lis_id] = dict()
        self.register[lis_id] = listener

    def start(self):
        for lis_id in self.register:
            self._report_manager.register(self.register[lis_id])
        self.running = True
        _thread.start_new_thread(self.recv_process, ())
        _thread.start_new_thread(self.send_process, ())
        while self.running:
            for key in self.config.get("display_camera", []):
                if not self.running:
                    break
                self.raw_image_lock.acquire()
                if key in self.raw_image_buffer and self.raw_image_buffer[key] is not None:
                    img = self.raw_image_buffer[key][:]
                else:
                    img = None
                self.raw_image_lock.release()
                if img is None:
                    continue
                for reg_id in self.register_id:
                    if key not in self.register_buffer[reg_id]:
                        continue
                    self.register_lock[reg_id].acquire()
                    buf = self.register_buffer[reg_id][key][:]
                    self.register_lock[reg_id].release()
                    self.register_buffer_local[reg_id][key] = buf
                    img = self.register[reg_id].draw(img, buf)
                cv2.imshow('Smart Lab - %s' % key, img)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    GV.register("ended", True)
                    break

    def stop(self):
        self.running = False
        self.receive_socket.stop()
        self.image_socket.stop()
        cv2.destroyAllWindows()

    def restart_socket(self, s):
        logger.info("Restarting socket")
        self.num_restart_socket += 1
        try:
            s.stop()
        except Exception:
            pass
        finally:
            s.start()
            logger.info("Socket successfully restarted!")

    def process_image(self, img):
        self.num_recv_from_PSI += 1
        if self.running:
            if 'camera_id' not in self.property:
                return
            pair = self.decode_msg(img, self.config.get("psi_image_format", "jpg"))
            if pair is None:
                return
            else:
                raw_img, base64_img = pair
            if self.raw_image_lock.acquire(blocking=False):
                self.raw_image_buffer[self.property['camera_id']] = raw_img
                self.raw_image_lock.release()
            if self.base64_image_lock.acquire(blocking=False):
                self.base64_image_buffer[self.property['camera_id']] = base64_img
                self.base64_image_lock.release()

    def send_property(self):
        for prop_name in self.property.keys():
            prop_value = self.property[prop_name]
            if prop_name == "timestamp":
                GV.register(f"visualizer.sendtime.{self.property['timestamp']}", time.time())
            if isinstance(prop_value, str):
                self.image_socket.send_str("%s:str:%s" % (prop_name, prop_value))
            elif isinstance(prop_value, int):
                self.image_socket.send_str("%s:int:%s" % (prop_name, prop_value))
            elif isinstance(prop_value, float):
                self.image_socket.send_str("%s:float:%s" % (prop_name, prop_value))
        self.image_socket.send_str("END")

    def send_process(self):
        self.image_socket.start()
        while self.running:
            if 'camera_id' not in self.property or self.property['camera_id'] not in self.base64_image_buffer:
                continue
            if self.base64_image_lock.acquire(blocking=False):
                img = self.base64_image_buffer[self.property['camera_id']]
                self.base64_image_lock.release()
                img = img.decode()
            else:
                continue
            while not self.ready_to_send:
                pass
            if img is None or len(img) == 0 or self.width == 0 or self.height == 0:
                self.ready_to_send = False
                return
            else:
                try:
                    self.image_socket.send_img(self.width, self.height, img, form='.jpg')
                    self.send_property()
                except ValueError:
                    logger.error(f"ValueError occurred when sending image")
                    logger.error(f"self.width: {self.width}")
                    logger.error(f"self.height: {self.height}")
                    logger.error(f"image data: {img}")
                    logger.error(f"image format: '.jpg'")
                    self.num_fail_send_to_server += 1
                except ConnectionAbortedError:
                    logger.warning("Connection to server stopped.")
                    self.restart_socket(self.image_socket)
                    self.num_fail_send_to_server += 1
                except Exception as e:
                    logger.warning(f"Undefined error type {type(e)} occured when sending image, traceback:",
                                   exc_info=True)
                    self.num_fail_send_to_server += 1
                finally:
                    self.num_send_to_server += 1
                    self.ready_to_send = False

    def recv_process(self):
        self.receive_socket.start()
        while self.running:
            try:
                topic = self.receive_socket.recv_str()
                logger.debug(topic)
                logger.debug(topic.split(":"))
                if topic.split(":")[0] == "type":
                    _, topic, cam_id = topic.split(":")
                    logger.debug(self.register_topic)
                    rid = self.register_topic[topic]
                    buf = self.register[rid].receive(self.receive_socket)[:]
                    self.register_lock[rid].acquire()
                    self.register_buffer[rid][cam_id] = buf
                    self.register_lock[rid].release()
                else:
                    logger.debug(topic.split(":")[0], "type", topic.split(":")[0] == "type")
                    continue
            except socket.timeout as e:
                pass
            except ValueError as e:
                logger.error(f"ValueError occurred when receiving data")
                self.restart_socket(self.receive_socket)
            except OSError as e:
                logger.info("Socket Closed.")

    def on_report_overall(self, overall_time, logger):
        logger.info(f"Received {self.num_recv_from_PSI} images from PSI. "
                    f"({self.num_recv_from_PSI / overall_time} per second)")
        logger.info(f"Sent {self.num_send_to_server} images to server. ({self.num_send_to_server / overall_time} "
                    f"per second)")
        logger.info(f"Among those, {self.num_fail_send_to_server} failed. "
                    f"({self.num_fail_send_to_server / overall_time} per second)")
        logger.info(f"Restarted sockets for {self.num_restart_socket} time(s).")

    def on_message(self, msg):
        super().on_message(msg)
