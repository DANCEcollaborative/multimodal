from common.GlobalVariables import GV
import logging
from termcolor import colored
import sys


class ColorfulFormatter(logging.Formatter):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

    def formatMessage(self, record):
        log = super().formatMessage(record)
        if record.levelno == logging.DEBUG:
            prefix = colored("DEBUG", "cyan")
        elif record.levelno == logging.INFO:
            prefix = colored("INFO ", "green")
        elif record.levelno == logging.WARNING:
            prefix = colored("WARN ", "yellow")
        elif record.levelno == logging.ERROR or record.levelno == logging.CRITICAL:
            prefix = colored("ERROR", "red")
        else:
            return log
        return prefix + " " + log


def get_logger(name: str="Realmodal", level: str=None, set_global: bool=False):
    if level is None:
        level = GV.get("logging.level", "info")
    else:
        if set_global:
            GV.register("logging.level", level)
    logger = logging.getLogger(name)
    logger.setLevel(level.upper())
    formatter = ColorfulFormatter(
        "%(asctime)s | %(name)s: " + "%(message)s",
        datefmt="%Y-%m-%dT%H:%M:%S",
    )
    console_handler = logging.StreamHandler(stream=sys.stdout)
    console_handler.setLevel(level.upper())
    console_handler.setFormatter(formatter)
    logger.addHandler(console_handler)
    return logger

