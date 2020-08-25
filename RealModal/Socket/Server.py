from utils.GlobalVaribles import GlobalVariables as GV

import threading
import _thread
from socketserver import ThreadingTCPServer, BaseRequestHandler
from socket import socket, AF_INET, SOCK_STREAM
from Socket.BaseSocket import BaseTCPSocket

from component.ServerProcessor import \
    BaseImageProcessor, FaceRecognitionProcessor, OpenPoseProcessor, PositionProcessor

from utils.LoggingUtil import logging


class Server():
    def __init__(self, addr=None, handler=None):
        self.addr = addr
        self.handler = handler
        self.server = None

    def start(self):
        self.server = ThreadingTCPServer(self.addr, self.handler)
        server_thread = threading.Thread(target=self.server.serve_forever, name=str(self.handler))
        server_thread.start()

    def stop(self):
        self.server.shutdown()


class DataTransmissionHandler(BaseRequestHandler, BaseTCPSocket):
    def setup(self):
        super().setup()
        self.event = threading.Event()
        self.BUF_SIZE = 4096
        self.tcp_socket = self.request

    def finish(self):
        super().finish()
        self.event.set()

    def handle(self):
        super().handle()


class SimpleTCPServer(BaseTCPSocket):
    def __init__(self, addr, buf_size=4096):
        super(SimpleTCPServer, self).__init__()
        self.addr = addr
        self.BUF_SIZE = buf_size
        self.socket_server = None
        self.tcp_socket = None
        self.timeout = None

    def start(self):
        if self.socket_server is None:
            self.socket_server = socket(AF_INET, SOCK_STREAM)
            self.socket_server.bind(self.addr)
            self.socket_server.listen(5)
        c, c_addr = self.socket_server.accept()
        print("New connection from address: ", c_addr)
        self.tcp_socket = c

    def stop(self, socket_only=True):
        if self.tcp_socket is not None:
            try:
                self.tcp_socket.close()
            except Exception as e:
                print(e)
        if not socket_only:
            if self.socket_server is not None:
                try:
                    self.socket_server.close()
                except Exception as e:
                    print(e)

    def restart(self):
        self.stop()
        self.start()


class ImageReceiveHandler(DataTransmissionHandler):
    def setup(self):
        super().setup()

    def finish(self):
        print("ImageReceiveHandler terminated a request from: ", self.client_address)
        super().finish()

    def handle(self):
        super().handle()
        self.start()

    def start(self):
        print("ImageReceiveHandler received a new request from: ", self.client_address)
        self.recv_process()

    def recv_process(self):
        while not self.event.is_set():
            try:
                img = self.recv_img()
                if img is None:
                    continue
                logging("Received Image:", img.shape)
                temp = self.recv_str()
                logging(temp)
                info = dict()
                info["img"] = img
                while temp != "END":
                    res = temp.split(":", 2)
                    if len(res) == 3:
                        prop_name, prop_type, prop_content = res
                        if prop_type == "str":
                            info[prop_name] = str(prop_content)
                        elif prop_type == "int":
                            info[prop_name] = int(prop_content)
                        elif prop_type == "float":
                            info[prop_name] = float(prop_content)
                    temp = self.recv_str()
                    logging(temp)

                for i, stat in enumerate(GV.ProcessorState):
                    if stat == "Available":
                        _thread.start_new_thread(GV.Processor[i].base_process, (info, i, self.client_address[0]))
            except (ConnectionResetError, ValueError, IOError) as e:
                print("Connection terminated")
                self.event.set()
                break
            except Exception as e:
                print(e)
                continue


class DataSendHandler(DataTransmissionHandler):
    def setup(self):
        super().setup()
        self.send_lock = threading.Lock()

    def finish(self):
        print("DataSendHandler terminated a request from: ", self.client_address)
        super().finish()

    def handle(self):
        super().handle()
        self.start()

    def start(self):
        print("DataSendHandler received a new request from: ", self.client_address)
        self.send_process()

    def send_process(self):
        while not self.event.is_set():
            for i, stat in enumerate(GV.ProcessorState):
                if stat == f"Pending:{self.client_address[0]}":
                    lock_flag = False
                    try:
                        if self.send_lock.acquire(blocking=False):
                            lock_flag = True
                            GV.Processor[i].base_send(self)
                    except (ConnectionResetError, ValueError, IOError) as e:
                        print("Connection terminated")
                        self.event.set()
                        break
                    except Exception as e:
                        print(e)
                    finally:
                        if lock_flag:
                            self.send_lock.release()
                            GV.ProcessorState[i] = "Available"
