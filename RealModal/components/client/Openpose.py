from common.GlobalVariables import GV
from components.client.RemoteListener import BaseRemoteListener
import cv2
import numpy as np
import base64

from Socket.Client import DataTransmissionClient as DTC
from common.logprint import get_logger

logger = get_logger(__name__)

@GV.register_listener("openpose")
class OpenPoseListener(BaseRemoteListener):
    def __init__(self, config):
        super(OpenPoseListener, self).__init__(config)
        self.num_receive = 0

    def receive(self, socket: DTC):
        self.num_receive += 1
        buf = []
        pose_num = socket.recv_int()
        logger.debug("%d pose(s) to receive." % pose_num)
        if pose_num > 0:
            data = socket.recv_data()
            decoded = base64.b64decode(data)
            array = np.fromstring(decoded, dtype=np.float32).reshape(pose_num, 25, 3)
            buf = array.tolist()
        return buf

    def draw(self, img, buf):
        for person in buf:
            for point in person:
                x, y, c = point
                if c < 1e-3:
                    continue
                cv2.circle(img, (int(x), int(y)), 3, (0, 255, 0), 1)
        return img

    def on_report_overall(self, overall_time, logger):
        logger.info(f"Openpose module received {self.num_receive} messages.")