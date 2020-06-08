import numpy as np
import base64
import cv2


class BaseTCPSocket():
    def __init__(self):
        self.tcp_socket = None
        self.BUF_SIZE = None
        self.timeout = None

    def set_timeout(self, sec):
        self.timeout = sec
        if self.timeout is not None and self.tcp_socket is not None:
            self.tcp_socket.settimeout(sec)

    def get_timeout(self):
        if self.tcp_socket is None:
            return None
        else:
            return self.tcp_socket.gettimeout()

    def send_data(self, bytedata):
        l = len(bytedata)
        to_send = str(l)
        while len(to_send) < 16:
            to_send = "0" + to_send
        self.tcp_socket.send(to_send.encode())
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
        l = int(self.tcp_socket.recv(16).decode())
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
        self.send_data(str(num).encode())

    def recv_int(self):
        return int(self.recv_data().decode())

    def send_float(self, num: float):
        self.send_data(str(num).encode())

    def recv_float(self):
        return float(self.recv_data().decode())

    def send_str(self, s: str):
        self.send_data(s.encode())

    def recv_str(self):
        return self.recv_data().decode()

    def send_img(self, width: int, height: int, img: str, form: str = "raw"):
        self.send_str("New Image")
        self.send_int(width)
        self.send_int(height)
        self.send_str(form)
        self.send_str(img)

    def recv_img(self):
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
