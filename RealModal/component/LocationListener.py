from utils.GlobalVaribles import GlobalVariables as GV
from utils.LoggingUtil import logging

from utils.PositionCalcUtil import *

from communication.BaseListener import BaseListener
from communication.CommunicationManager import CommunicationManager as CM


class LocationQuerier(BaseListener):
    def __init__(self, cm: CM, topic_in=None, topic_out=None):
        super(LocationQuerier, self).__init__(cm)
        self.topic_in = topic_in
        self.topic_out = topic_out
        self.state = "Available"
        self.result = None
        self.subscribe_to(self.topic_in)

    def query(self, cid: str, timestamp: int, pixel: Point2D):
        self.state = "Querying"
        print("Send to topic: ", self.topic_out)
        print(f"Content: {timestamp};{pixel.x};{pixel.y}")
        self.cm.send(self.topic_out, f"{timestamp};{pixel.x};{pixel.y}")
        while self.state != "Pending":
            pass
        self.state = "Available"
        return self.result

    def on_message(self, headers, msg):
        # TODO: add support for more cameras
        print("Get location message from PSI:", msg)
        msg_split = msg.split(";")
        if msg_split[1] == "null":
            # Cannot get the location information from depth camera
            self.result = p_zero()
        else:
            timestamp, x, y, z = msg.split(';')
            # transform to centimeters.
            self.result = Point3D(
                float(x),
                float(y),
                float(z)
            )
        self.state = "Pending"
