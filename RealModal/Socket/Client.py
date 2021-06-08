from socket import socket, AF_INET, SOCK_STREAM
from Socket.BaseSocket import BaseTCPSocket
import base64
import numpy as np


class DataTransmissionClient(BaseTCPSocket):
    """
    This class defines a encapsulated TCP Client.
    """
    def __init__(self, addr, buf_size=4096):
        """
        Initialization of the client.
        :param addr: the address of the server to be connected.
        :param buf_size: the size of the buffer used to send/receive the byte message.
        """
        super().__init__()
        self.addr = addr
        self.BUF_SIZE = buf_size
        self.tcp_socket = None
        self.timeout = None

    def start(self):
        """
        Try to connect to the TCP Server running at self.addr.
        :return: None
        """
        self.tcp_socket = socket(AF_INET, SOCK_STREAM)
        if self.timeout is not None:
            self.tcp_socket.settimeout(self.timeout)
        self.tcp_socket.connect(self.addr)

    def stop(self):
        """
        Close the socket.
        :return: None
        """
        self.tcp_socket.close()

