from common.GlobalVariables import GV
from components.server.ServerProcessor import BaseImageProcessor
from utils.FaceRecognitionUtil import FaceRecognitionUtil as FRU
from Socket.BaseSocket import BaseTCPSocket
from common.logprint import get_logger

import threading

logger = get_logger(__name__)


@GV.register_processor("face_recognition")
class FaceRecognitionProcessor(BaseImageProcessor):
    def __init__(self, config):
        super(FaceRecognitionProcessor, self).__init__(config)
        self.recognizer = GV.get("util.face_recognition")
        assert self.recognizer is not None, \
            "Face Recognizer should be initialized before using!"
        self.face_id = []
        self.face_loc = []

    @classmethod
    def initialize(cls, config):
        if GV.get("util.face_recognition", None) is None:
            GV.register("util.face_recognition", FRU())
            GV.register("lock.face_recognition", threading.Lock())

    def process(self, info):
        img = info['img']
        if img.shape[-1] == 4:
            img = img[:, :, :3]
        self.face_id, self.face_loc = self.recognizer.recognize(img)
        GV.get("lock.face_recognition").acquire()
        GV.register(
            f"result.face_recognition.{info['camera_id']}",
            (self.face_id, self.face_loc)
        )
        GV.get("lock.face_recognition").release()

    def send(self, soc: BaseTCPSocket):
        l = len(self.face_id)
        logger.debug("find %d face(s) in the image" % l)
        soc.send_int(l)
        for i in range(l):
            logger.debug("sending face %d" % i)
            soc.send_int(self.face_id[i])
            soc.send_int(self.face_loc[i][0])
            soc.send_int(self.face_loc[i][1])
            soc.send_int(self.face_loc[i][2])
            soc.send_int(self.face_loc[i][3])
