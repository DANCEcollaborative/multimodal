from DialogAgents.BaseAgent import BaseAgent
from common.logprint import get_logger

logger = get_logger(__name__)

class FileReaderAgent(BaseAgent):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        filename = kwargs["filename"]
        try:
            with open(filename, 'r', encoding='UTF-8') as fin:
                self.scripts = fin.readlines()
        except Exception as e:
            logger.warning(f"Exception {type(e)} occurred when loading scripts, traceback:", exc_info=True)
            self.scripts = []
        logger.debug(self.scripts)
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


