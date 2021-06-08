import numpy as np
import base64
import cv2


class BaseTCPSocket():
    """
    This class defines some basic TCP transmitting logic. All the sending and receiving logic is encapsulated. Derive
    this class to use these sending/receiving methods.
    """
    def __init__(self):
        """
        Initialize a TCPSocket with most field leaving blank. These fields should be initialized as soon as it's being
        created. Refer to the use of a socket to understand the meaning of the fields or see the examples in Client.py
        and Server.py
        """
        self.tcp_socket = None
        self.BUF_SIZE = None
        self.timeout = None

    def set_timeout(self, sec):
        """
        Set the time for the socket to wait before raising a timeout exception.
        :param sec: Seconds waited for.
        :return: None
        """
        self.timeout = sec
        if self.timeout is not None and self.tcp_socket is not None:
            self.tcp_socket.settimeout(sec)

    def get_timeout(self):
        """
        Get the time of the socket to wait before raising a timeout exception.
        :return: Seconds waited for.
        """
        if self.tcp_socket is None:
            return None
        else:
            return self.tcp_socket.gettimeout()

    def send_data(self, bytedata):
        """
        Basic method to send byte arrays.
        In this method, the length of the byte array will be sent before the contents. In order to maintain the
        consistency, the receiver must use the recv_data method in corresponding placesto avoid
        :param bytedata: the byte array to be sent via tcp socket.
        :return: int
            Bytes successfully sent by the socket.
        """
        # Send the length of the byte array.
        l = len(bytedata)
        to_send = str(l)
        while len(to_send) < 16:
            to_send = "0" + to_send
        self.tcp_socket.send(to_send.encode())

        # Send the content.
        current = 0
        try:
            while True:
                if current + self.BUF_SIZE >= l:
                    self.tcp_socket.send(bytedata[current:])
                    break
                else:
                    self.tcp_socket.send(bytedata[current:current+self.BUF_SIZE])
                    current += self.BUF_SIZE
        finally:
            return current

    def recv_data(self):
        """
        Receive a byte array from the socket.
        This method is paired with send_data. It will first receive the length of the data, then receive exactly length
        of data. In this way, sticky packets will be avoided.
        :return: bytearray
            The byte array received from the socket.
        """
        # Receive the length of the data.
        l = int(self.tcp_socket.recv(16).decode())

        # Receive data of the exact length.
        current_data = b''
        current_len = 0
        while True:
            if current_len + self.BUF_SIZE >= l:
                data = self.tcp_socket.recv(l - current_len)
                current_data += data
                break
            else:
                data = self.tcp_socket.recv(self.BUF_SIZE)
                current_data += data
                current_len += len(data)
        return current_data

    def send_int(self, num: int):
        """
        Send an integer via the socket.
        :param num: The integer to be sent.
        :return: None
        """
        self.send_data(str(num).encode())

    def recv_int(self):
        """
        Receive an integer via the socket.
        :return: int
            The integer received.
        """
        return int(self.recv_data().decode())

    def send_float(self, num: float):
        """
        Send a float via the socket.
        :param num: The float to be sent.
        :return: None
        """
        self.send_data(str(num).encode())

    def recv_float(self):
        """
        Receive a float via the socket.
        :return: float
            The float received.
        """
        return float(self.recv_data().decode())

    def send_str(self, s: str):
        """
        Send a string via the socket.
        :param s: The string to be sent.
        :return: None
        """
        self.send_data(s.encode())

    def recv_str(self):
        """
        Receive a string via the socket.
        :return: string
            The string Received.
        """
        return self.recv_data().decode()

    def send_img(self, width: int, height: int, img: str, form: str = "raw"):
        """
        Send an image data via the socket. Before sending, the img data should be encoded by b64encode to ensure that
        every byte to be sent is valid.
        :param width: the width of the image.
        :param height: the height of the image.
        :param img: the base64 encoded data of the original image.
        :param form: the format of the image. Set to raw to send the image as pixels. Otherwise, set it to the original
        image format such as "jpg", "png", etc.
        :return: None
        """
        self.send_str("New Image")
        self.send_int(width)
        self.send_int(height)
        self.send_str(form)
        self.send_str(img)

    def recv_img(self):
        """
        Receive a image from the socket. This method should be used in pair of the send_img method.
        :return: numpy.array
            The numpy array containing the decoded img data.
        """
        text = self.recv_str()
        while text != "New Image":
            text = self.recv_str()
        width = self.recv_int()
        height = self.recv_int()
        form = self.recv_str()
        raw_data = self.recv_str()
        b = base64.b64decode(raw_data)
        img_data = np.frombuffer(b, dtype=np.uint8)
        if form == "raw":
            img = img_data.reshape(height, width, -1)
        else:
            img = cv2.imdecode(img_data, cv2.IMREAD_COLOR)
        return img
