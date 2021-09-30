from common.GlobalVariables import GV
from components.client.RemoteListener import BaseRemoteListener
import cv2

from Socket.Client import DataTransmissionClient as DTC
from common.logprint import get_logger

logger = get_logger(__name__)

@GV.register_listener("face_recognition")
class FaceRecognitionListener(BaseRemoteListener):
    def __init__(self, config):
        super(FaceRecognitionListener, self).__init__(config)

    def receive(self, socket: DTC):
        buf = []
        face_num = socket.recv_int()
        print("%d face(s) to receive." % face_num)
        for i in range(face_num):
            print("receiving face %d" % i)
            fid = socket.recv_int()
            loc1 = socket.recv_int()
            loc2 = socket.recv_int()
            loc3 = socket.recv_int()
            loc4 = socket.recv_int()
            buf.append((fid, (loc1, loc2, loc3, loc4)))
        logger.debug(buf)
        return buf

    def draw(self, img, buf):
        # logging("Face Recognition drawing...")
        for fid, (top, right, bottom, left) in buf:
            cv2.rectangle(img, (left, top), (right, bottom), (0, 255, 0), 1)
            cv2.rectangle(img, (left, bottom - 25), (right, bottom), (0, 255, 0), cv2.FILLED)
            cv2.putText(img, "face %d" % fid, (left + 6, bottom - 6), cv2.FONT_HERSHEY_DUPLEX, 0.5, (255, 255, 255), 1)
        return img
