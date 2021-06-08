import json
import random


class IdiomUtil():
    def __init__(self):
        with open("data/chengyu_utf8.json", encoding="utf8") as fin:
            self.idioms = json.load(fin)
        self.pinyin2idioms = {}
        for idiom in self.idioms:
            start_py = idiom[1].split()[0]
            if start_py not in self.pinyin2idioms:
                self.pinyin2idioms[start_py] = []
            self.pinyin2idioms[start_py].append(idiom[0])

        self.idioms2pinyin = {}
        for idiom in self.idioms:
            self.idioms2pinyin[idiom[0]] = idiom[1]

    def has_idiom(self, idiom):
        return idiom in self.idioms2pinyin

    def get_next_idiom(self, idiom):
        end_py = self.idioms2pinyin[idiom].split()[-1]
        return random.choice(self.pinyin2idioms[end_py])