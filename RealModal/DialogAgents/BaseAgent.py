import abc


class BaseAgent(metaclass=abc.ABCMeta):
    def __init__(self, *args, **kwargs):
        pass

    @abc.abstractmethod
    def respond(self, s: str) -> str:
        pass
