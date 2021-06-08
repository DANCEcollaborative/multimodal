from utils.GlobalVariables import GlobalVariables as GV

import threading
import _thread
from socketserver import ThreadingTCPServer, BaseRequestHandler
from socket import socket, AF_INET, SOCK_STREAM
from Socket.BaseSocket import BaseTCPSocket

from component.ServerProcessor import \
    BaseImageProcessor, FaceRecognitionProcessor, OpenPoseProcessor, PositionProcessor

from utils.LoggingUtil import logging


class Server():
    """
    Create a TCP server instance. The behavior of the server is defined by the handler.
    """
    def __init__(self, addr=None, handler=None):
        self.addr = addr
        self.handler = handler
        self.server = None

    def start(self):
        """
        Start the server in a new thread so it won't block the main thread. Refer to the use of ThreadingTCPServer for
        more details.
        :return: None
        """
        self.server = ThreadingTCPServer(self.addr, self.handler)
        server_thread = threading.Thread(target=self.server.serve_forever, name=str(self.handler))
        server_thread.start()

    def stop(self):
        """
        Stop the server.
        :return:
        """
        self.server.shutdown()


class SimpleTCPServer(BaseTCPSocket):
    """
    Defines a simple TCP Server which only used for sending and receive data. This means it doesn't have a handler in
    the backend so it won't do any processing within this class.
    """
    def __init__(self, addr, buf_size=4096):
        super(SimpleTCPServer, self).__init__()
        self.addr = addr
        self.BUF_SIZE = buf_size
        self.socket_server = None
        self.tcp_socket = None
        self.timeout = None

    def start(self):
        """
        Start the server and wait for clients.
        :return: None
        """
        if self.socket_server is None:
            self.socket_server = socket(AF_INET, SOCK_STREAM)
            self.socket_server.bind(self.addr)
            self.socket_server.listen(5)
        c, c_addr = self.socket_server.accept()
        print("New connection from address: ", c_addr)
        self.tcp_socket = c

    def stop(self, socket_only=True):
        """
        Stop the connection between sockets. If socket_only is set True, the server will also be stopped and won't
        accept further connection requests.
        :param socket_only: whether to only stop the sockets or stop the server as well.
        :return: None
        """
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
        """
        Try to restart the server. Call this method when some network error occurred.
        :return: None
        """
        self.stop()
        self.start()


class DataTransmissionHandler(BaseRequestHandler, BaseTCPSocket):
    """
    Define a basic handler which aims to send and receive data from clients.
    Different from SimpleTCPServer, it:
    1. must be used as a component of the Server class.
    2. will try to find an available port to create a new socket. This port cannot be defined in advance.
    Refer to BaseRequestHandler for more details.
    """
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


class ImageReceiveHandler(DataTransmissionHandler):
    """
    Define a handler which aims to receive image data from the local machine.
    """
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
        """
        Constantly listening to the tcp socket to get images, and distribute it to the processors.
        :return: None
        """
        while not self.event.is_set():
            try:
                # Constantly listening to the socket until a new image is coming.
                img = self.recv_img()
                if img is None:
                    continue
                logging("Received Image:", img.shape)

                # Receive the properties of the image including timestamp, camera_id and other user-defined properties.
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

                # Check the processors. Feed data to free processors in another thread.
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
    """
    Define a handler which aims to send the processed results back to the local machine.
    """
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
