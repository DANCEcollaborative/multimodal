from common.GlobalVariables import GV
from components.server.ServerProcessor import BaseImageProcessor
from Socket.BaseSocket import BaseTCPSocket
from utils.OpenPoseUtil import OpenPoseUtil as OPU

import threading
import base64
from common.logprint import get_logger

logger = get_logger(__name__)

@GV.register_processor("openpose")
class OpenPoseProcessor(BaseImageProcessor):
    def __init__(self, config):
        super(OpenPoseProcessor, self).__init__(config)
        self.openpose = GV.get("util.openpose")
        assert self.openpose is not None, \
            "OpenPose should be initialized before using!"
        self.poseKeypoints = None

    @classmethod
    def initialize(cls, config):
        if GV.get("util.openpose") is None:
            GV.register("util.openpose", OPU(config.get("path")))
            GV.register("lock.openpose", threading.Lock())

    def process(self, info):
        img = info['img']
        if img.shape[-1] == 4:
            img = img[:, :, :3]
        self.poseKeypoints, _ = self.openpose.find_pose(img)
        logger.debug(type(self.poseKeypoints))
        GV.get("lock.openpose").acquire()
        GV.register(
            f"result.openpose.{info['camera_id']}",
            self.poseKeypoints.copy()
        )
        GV.get("lock.openpose").release()

    def send(self, soc: BaseTCPSocket):
        if len(self.poseKeypoints.shape) == 3:
            l = self.poseKeypoints.shape[0]
        else:
            l = 0
        logger.debug("find %d person(s) in the image" % l)
        soc.send_int(l)
        # Send poses only when there are at least one person detected.
        # This is because self.poseKeypoints will be a very weird value if no people is detected due to some features
        # or bugs in the  Openpose Library.
        if l > 0:
            soc.send_data(base64.b64encode(self.poseKeypoints.tostring()))
