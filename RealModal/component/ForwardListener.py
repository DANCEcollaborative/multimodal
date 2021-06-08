from communication.BaseListener import ImageListener
from utils.GlobalVariables import GlobalVariables as GV

import _thread
import abc
import base64
import cv2
import numpy as np
import socket
import threading

from communication.CommunicationManager import CommunicationManager as CM
from component.RemoteListener import BaseRemoteListener
from utils.LoggingUtil import logging
from Socket.Client import DataTransmissionClient as DTC

import time


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
            pair = self.decode_msg(img, GV.PSIImageFormat)
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
                GV.frame_process_time[self.property["timestamp"]] = time.time()
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
                    print(self.register_topic)
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

