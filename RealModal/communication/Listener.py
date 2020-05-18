from utils.GlobalVaribles import GlobalVariables as GV

import _thread
import abc
import base64
import cv2
import numpy as np
import socket
import threading

from .CommunicationManager import CommunicationManager as CM
from component.RemoteListener import BaseRemoteListener
from utils.LoggingUtil import logging
from Socket.Client import DataTransmissionClient as DTC

import time

class BaseListener(metaclass=abc.ABCMeta):
    """
    Abstract base class for all the listeners receiving messages from stomp.
    You could define your own class for handling stomp message but I strongly recommend you to derive from this class.
    """
    def __init__(self, cm: CM):
        """
            Initialization of a listener.
        :param cm: CommunicationManager
            Communication manager you're using.
        """
        self.cm = cm

    def subscribe_to(self, topic):
        """
            An alias to cm.subscribe() which omit the listener parameter.
        :param topic: str
            The subscribed topic.
        """
        self.cm.subscribe(self, topic)

    def unsubscribe_to(self, topic):
        """
            An alias to cm.unsbuscribe() which omit the listener parameter.
        :param topic: str
            The topic to unsubscribe
        """
        self.cm.unsubscribe(self, topic)

    @staticmethod
    def parse_topic(headers):
        """
            Parse the topic from the header.
            Since the original stomp protocol will pack the topic in ActiveMQ with /topic/, this method will get the
            real topic sent by ActiveMQ.
        :param headers: dict
            The header of the message. The `destination` field must be reserved.
        :return: str
            The true topic of a message.
        """
        return headers['destination'].split('/', 2)[-1]

    @abc.abstractmethod
    def on_message(self, headers, msg):
        """
            An abstract method to handle incoming message. Every subclass that is not a abstract class should implement
            this method.
        :param headers: dict
            The stomp protocol header in dict form. Keys are field names and values are contents.
        :param msg: str
            The message sent by stomp protocol.
        """
        pass


class ImageListener(BaseListener, metaclass=abc.ABCMeta):
    """
        The class to handle image message only.
        This class is the base class of all the listeners which specifically listen to the image messages.
        To fulfill a image message, the `size/property/image` data should be sent through different topics.
        The `*_Size` topic is for determine the width and height of the image, The `*_Image` topic is for the raw image
        data, and `*_Prop` is for additional properties including camera_id, timestamp and whatever you think is needed.
    """
    def __init__(self, cm, topic=None, decode=True):
        """
            Initialization of a ImageListener.
            Call this base initialization method when you're initialize your class to ensure the topics are properly
            subscribed to.
        :param cm: CommunicationManager
            The communication manager you're using.
        :param topic: str
            The topic name you're using.
            Do not include `_Image` suffix, they'll be automatically added.
        :param decode: bool
            If set `True`, The image will be decoded to real image data(np.array) before passing to `process_image`.
            Otherwise, The base64 format will be preserved.
        """
        super(ImageListener, self).__init__(cm)
        self.receiveLock = threading.Lock()
        if topic is not None:
            self.image_topic = topic + "_Image"
            self.size_topic = topic + "_Size"
            self.property_topic = topic + "_Prop"
            self.subscribe_to(self.image_topic)
            self.subscribe_to(self.size_topic)
            self.subscribe_to(self.property_topic)
        else:
            self.image_topic = None
            self.size_topic = None
            self.property_topic = None
        self.height = None
        self.width = None
        self.timestamp = None
        self.decode = decode
        self.property = dict()

    def set_image_topic(self, topic):
        """
            Specify the topic for transmitting image data.
            You can use this method (and the below two) to set the topic which may violate the limit of adding `_Image`,
            `_Size` and `_Prop` suffix.
        :param topic: str
            The topic specified for image data.
        """
        if self.image_topic is not None:
            self.unsubscribe_to(self.image_topic)
        self.image_topic = topic
        self.subscribe_to(topic)

    def set_size_topic(self, topic):
        """
            Specify the topic for transmitting size data.
        :param topic: str
            The topic specified for size data.
        """
        if self.size_topic is not None:
            self.unsubscribe_to(self.image_topic)
        self.size_topic = topic
        self.subscribe_to(topic)

    def set_property_topic(self, topic):
        """
            Specify the topic for transmitting property data.
        :param topic: str
            The topic specified for property data.
        """
        if self.property_topic is not None:
            self.unsubscribe_to(self.property_topic)
        self.property_topic = topic
        self.subscribe_to(topic)

    @abc.abstractmethod
    def process_image(self, img):
        """
            Process the image.
            This should be implemented by every subclass to process the image in some way.
        :param img: str / numpy.array->shape(height, width, num_channel)
            The image to process.
            If `self.decode` is True, it will be a decoded numpy.array with a shape of (height, width, num_channel).
            Otherwise it will be a base64 string represent the image data.
        """
        pass

    def process_property(self, prop_str: str):
        """
            Process the property string.
            A default valid property string should be `NAME:TYPE:CONTENT`. `TYPE` should be one of {str, int, float}.
            If a valid property string is detected, it will be automatically processed and saved to self.property as
            self.property[NAME] = TYPE(CONTENT).
            You can also define your own format of property string by overwriting this method.
        :param prop_str: str
            The property string to process.
        :return: bool
            Whether the string is a default property string and saved to the property dict.
        """
        res = prop_str.split(":", 2)
        if len(res) < 3:
            return False
        prop_name, prop_type, prop_content = res
        if prop_type == "str":
            self.property[prop_name] = str(prop_content)
        elif prop_type == "int":
            self.property[prop_name] = int(prop_content)
        elif prop_type == "float":
            self.property[prop_name] = float(prop_content)
        else:
            return False
        return True

    def process_size(self, size):
        """
            Parse the size message and save the width/height information.
            The size message should be `WIDTH:HEIGHT`.
        :param size: str
            The size string to parse.
        """
        w, h = size.split(':')
        self.width = int(w)
        self.height = int(h)

    def decode_msg(self, msg):
        """
            Try to decode a base64 message to a image.
        :param msg:
            The base64 message to decode.
        :return: img: numpy.array->shape(height, width, num_chanel)
            Gray 8bit/BGR 24bit/BGRA 32bit image array, capable with cv2.
            If decoding process failed, return None instead.
        """
        b = base64.b64decode(msg)
        try:
            img = np.frombuffer(b, dtype=np.uint8).reshape(self.height, self.width, -1)
        # TODO: specify possible exceptions here.
        except:
            img = None
        return img

    def process_message(self, headers, msg):
        """
            Parse the incoming message. Classify it to image/size/property and call the corresponding method for
            further processing.
            At most 1 message will be processed at a time by this method to prevent conflicts and limit the resource
            occupation. Incoming message will be discard if this listener is processing another message.
            TODO: add a queue and set an adjustable limitation to the number of messages that can be processed at the
            same time.
        :param headers: dict
            Header of the stomp message containing the topic information.
        :param msg: str
            The message to be classified.
        """
        flag = self.receiveLock.acquire(blocking=False)
        topic = self.parse_topic(headers)
        if flag:
            if topic == self.image_topic:
                if self.decode:
                    img = self.decode_msg(msg)
                else:
                    img = msg
                self.process_image(img)
            elif topic == self.size_topic:
                self.process_size(msg)
            elif topic == self.property_topic:
                self.process_property(msg)
            self.receiveLock.release()

    def on_message(self, headers, msg):
        """
            Derived from BaseListener.
            Start a new thread to process the message.
        :param headers:
            Header of the stomp message containing the topic information.
        :param msg:
            The message to process.
        """
        _thread.start_new_thread(self.process_message, (headers, msg))


class ForwardVisualizer(ImageListener):
    """
        This class is designed for processing image data in real-time. Generally it has the following few functions:
        1. Receive image data from ActiveMQ (derived from ImageListener).
        2. Forward image data to the server.
        3. Manage image processing components.
        4. Visualize image data for selected cameras.
    """
    def __init__(self, cm: CM, addr_in: tuple, addr_out: tuple, topic=None):
        super(ForwardVisualizer, self).__init__(cm, topic, decode=False)
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

    def process_property(self, prop_str: str):
        flag = super().process_property(prop_str)
        if not flag:
            if prop_str == "END":
                self.ready_to_send = True

    def add(self, listener: BaseRemoteListener, topic=None):
        if topic is None:
            topic = listener.topic
        lis_id = id(listener)
        if topic in self.register_topic:
            print("Change listener for topic %s" % topic)
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
        self.running = True
        _thread.start_new_thread(self.recv_process, ())
        _thread.start_new_thread(self.send_process, ())
        while self.running:
            for key in GV.CameraToDisplay:
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
                    self.stop()
                    break

    def stop(self):
        self.running = False
        self.receive_socket.stop()
        self.image_socket.stop()
        cv2.destroyAllWindows()
        GV.ended = True

    @staticmethod
    def restart_socket(s):
        print("Restarting socket...", end=" ")
        try:
            s.stop()
        except Exception:
            pass
        finally:
            s.start()
            print("Success!")

    def process_image(self, img):
        if self.running:
            if 'camera_id' not in self.property:
                return
            if self.raw_image_lock.acquire(blocking=False):
                decoded_img = self.decode_msg(img)
                self.raw_image_buffer[self.property['camera_id']] = decoded_img
                self.raw_image_lock.release()
            if self.base64_image_lock.acquire(blocking=False):
                self.base64_image_buffer[self.property['camera_id']] = img
                self.base64_image_lock.release()

    def send_property(self):
        for prop_name in self.property.keys():
            prop_value = self.property[prop_name]
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
            time0 = time.time()
            if 'camera_id' not in self.property or self.property['camera_id'] not in self.base64_image_buffer:
                continue
            if self.base64_image_lock.acquire(blocking=False):
                img = self.base64_image_buffer[self.property['camera_id']]
                self.base64_image_lock.release()
                # convert to .jpg to minimum image size.
                img = self.decode_msg(img)
                if img is None:
                    continue
                img = base64.b64encode(cv2.imencode(".jpg", img)[1]).decode()
            else:
                continue
            while not self.ready_to_send:
                pass
            if img is None or len(img) == 0 or self.width == 0 or self.height == 0:
                self.ready_to_send = False
                return
            else:
                try:
                    time1 = time.time()
                    self.image_socket.send_img(self.width, self.height, img, form='.jpg')
                    self.send_property()
                    time2 = time.time()
                except ValueError:
                    print("ValueError occurred.")
                except ConnectionAbortedError:
                    print("Connection to server stopped.")
                    self.restart_socket(self.image_socket)
                except Exception as e:
                    print(e)
                finally:
                    self.ready_to_send = False

    def recv_process(self):
        self.receive_socket.start()
        while self.running:
            try:
                topic = self.receive_socket.recv_str()
                logging(topic)
                logging(topic.split(":"))
                if topic.split(":")[0] == "type":
                    _, topic, cam_id = topic.split(":")
                    rid = self.register_topic[topic]
                    buf = self.register[rid].receive(self.receive_socket)[:]
                    self.register_lock[rid].acquire()
                    self.register_buffer[rid][cam_id] = buf
                    self.register_lock[rid].release()
                else:
                    logging(topic.split(":")[0], "type", topic.split(":")[0] == "type")
                    continue
            except socket.timeout as e:
                print(e)
                continue
            except ValueError as e:
                self.restart_socket(self.receive_socket)
            except OSError as e:
                print("Socket Closed.")
