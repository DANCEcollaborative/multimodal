from components.messenger.BaseMessenger import TextMessenger
from common.GlobalVariables import GV
from DialogAgents import agent_build_helper
from common.logprint import get_logger

logger = get_logger(__name__)


@GV.register_messenger("dialog_messenger")
# TODO: add supports for Dialog agents
class DialogMessenger(TextMessenger):
    def __init__(self, config):
        if not config.active:
            return
        config.topic = config.topic_in
        super(DialogMessenger, self).__init__(config)
        self.topic_out = self.config.topic_out
        self.config.kwargs["Listener"] = self
        # self.agent = agent_build_helper(*args, **kwargs)

    def process_text(self, text):
        logger.debug(text)
        content = dict()
        if text.split(";%;")[0] == "multimodal:true":
            for kv in text.split(";%;"):
                k, v = kv.split(":")
                content[k] = v
        else:
            content["speech"] = text
        if self.topic_out is not None:
            response = self.agent.respond(content["speech"])
            if response != "":
                response = response.encode('utf-8')
                logger.debug(f"sending message through {self.topic_out}: {response.decode()}")
                self.send(self.topic_out, response)

    def send_text(self, text):
        """
        Actively send a message through the communication manager
        :return: None
        """
        text = text.encode('utf-8')
        logger.debug(text)
        self.send(self.topic_out, text)
