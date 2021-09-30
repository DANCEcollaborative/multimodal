from Socket.BaseSocket import BaseTCPSocket
from common.GlobalVariables import GV
from common.Report import ReportCallback

import abc


@GV.register_listener("base")
class BaseRemoteListener(ReportCallback, metaclass=abc.ABCMeta):
    def __init__(self, config):
        super().__init__()
        self.config = config
        self.topic = config.get("topic", None)

    @staticmethod
    def receive_properties(socket: BaseTCPSocket) -> dict:
        prop_dict = dict()
        prop_str = socket.recv_str()
        while prop_str != "END":
            res = prop_str.split(":", 2)
            if len(res) == 3:
                prop_name, prop_type, prop_content = res
                if prop_type == "str":
                    prop_dict[prop_name] = str(prop_content)
                elif prop_type == "int":
                    prop_dict[prop_name] = int(prop_content)
                elif prop_type == "float":
                    prop_dict[prop_name] = float(prop_content)
            prop_str = socket.recv_str()
            # print(prop_str)
        return prop_dict

    @abc.abstractmethod
    def receive(self, socket):
        pass

    @abc.abstractmethod
    def draw(self, img, buf):
        pass
