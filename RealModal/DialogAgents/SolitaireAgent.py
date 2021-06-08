from utils.GlobalVariables import GlobalVariables as GV
from utils.IdiomUtil import IdiomUtil
from DialogAgents.BaseAgent import BaseAgent
import random


class SolitaireAgent(BaseAgent):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        if GV.IdiomUtil is None:
            GV.IdiomUtil = IdiomUtil()
        self.idiom_util = GV.IdiomUtil
        self.success_template = [
            "真厉害！那我接%s。",
            "漂亮！该我了，%s。"
        ]
        self.fail_template = [
            "%s不是个成语哦，再想想吧。",
            "抱歉，%s并不是个成语，换一个吧。"
        ]

    def respond(self, s: str) -> str:
        s = ''.join(s.strip().split())
        if self.idiom_util.has_idiom(s):
            next_idiom = self.idiom_util.get_next_idiom(s)
            template = random.choice(self.success_template)
            return template % next_idiom
        else:
            template = random.choice(self.fail_template)
            return template % s
