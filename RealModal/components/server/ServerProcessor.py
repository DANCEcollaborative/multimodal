from common.GlobalVariables import GV

import time
import abc
import threading

from Socket.BaseSocket import BaseTCPSocket
from common.Report import ReportCallback
from common.logprint import get_logger

logger = get_logger(__name__)


@GV.register_processor("base")
class BaseImageProcessor(ReportCallback, metaclass=abc.ABCMeta):
    def __init__(self, config):
        super().__init__()
        topic = config.get("topic", None)
        self.topic = topic
        process_interval = config.get("process_interval", 0.5)
        self.process_interval = process_interval
        self.current = None
        self.last_time = 0

    @classmethod
    def initialize(cls, config):
        pass

    def base_send(self, soc: BaseTCPSocket):
        soc.send_str(f"type:{self.topic}:{self.current['camera_id']}")
        try:
            self.send(soc)
        except Exception as e:
            logger.warning(f"Exception {type(e)} occurred when sending messages, traceback:", exc_info=True)

    @abc.abstractmethod
    def send(self, soc: BaseTCPSocket):
        pass

    def base_process(self, info, pos, ip_addr):
        if time.time() - self.last_time < self.process_interval:
            return
        if GV.get("processor.lock")[pos].acquire(blocking=False):
            GV.get("processor.state")[pos] = f"Processing:{ip_addr}"
            self.current = info.copy()
            self.last_time = time.time()
            try:
                self.process(info)
                GV.get("processor.state")[pos] = f"Pending:{ip_addr}"
            except Exception as e:
                logger.warning(f"Exception {type(e)} occurred when processing data, traceback:", exc_info=True)
                GV.get("processor.state")[pos] = "Available"
            finally:
                GV.get("processor.lock")[pos].release()

    @abc.abstractmethod
    def process(self, info):
        pass




