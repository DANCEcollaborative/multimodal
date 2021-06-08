from DialogAgents.BaseAgent import BaseAgent
from utils.LoggingUtil import logging


class FileReaderAgent(BaseAgent):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        filename = kwargs["filename"]
        try:
            with open(filename, 'r', encoding='UTF-8') as fin:
                self.scripts = fin.readlines()
        except Exception as e:
            print(e)
            self.scripts = []
        logging(self.scripts)
        self.current = 0

    def respond(self, s: str) -> str:
        if s == "<start>":
            self.current = 0
        if s == "<next>" or s == "<start>":
            if self.current >= len(self.scripts):
                return ""
            else:
                to_ret = self.scripts[self.current].strip()
                self.current += 1
                return to_ret
        return ""


