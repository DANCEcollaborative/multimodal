from socket import socket, AF_INET, SOCK_STREAM
from Socket.BaseSocket import BaseTCPSocket
import base64
import numpy as np


class DataTransmissionClient(BaseTCPSocket):
    def __init__(self, addr, buf_size=4096):
        super().__init__()
        self.addr = addr
        self.BUF_SIZE = buf_size
        self.tcp_socket = None
        self.timeout = None

    def start(self):
        self.tcp_socket = socket(AF_INET, SOCK_STREAM)
        if self.timeout is not None:
            self.tcp_socket.settimeout(self.timeout)
        self.tcp_socket.connect(self.addr)

    def stop(self):
        self.tcp_socket.close()

