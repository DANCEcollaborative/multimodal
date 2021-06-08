from communication.BaseListener import TextListener
from utils.GlobalVariables import GlobalVariables as GV
from DialogAgents import agent_build_helper


class DialogListener(TextListener):
    def __init__(self, cm=None, topic_in=None, topic_out=None, *args, **kwargs):
        if cm is None:
            cm = GV.manager
        super(DialogListener, self).__init__(cm, topic_in)
        self.topic_out = topic_out
        kwargs["Listener"] = self
        self.agent = agent_build_helper(*args, **kwargs)

    def process_text(self, text):
        print(text)
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
                print(f"sending message through {self.topic_out}: {response.decode()}")
                self.cm.send(self.topic_out, response)

    def send_text(self, text):
        """
        Actively send a message through the communication manager
        :return: None
        """
        text = text.encode('utf-8')
        print(text)
        self.cm.send(self.topic_out, text)
