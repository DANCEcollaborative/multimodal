from common.logprint import get_logger
from common.Configuration import load_yaml

if __name__ == "__main__":
    # Get configurations
    # TODO: add support for argparse
    config_path = "config/config.yaml"
    config = load_yaml(config_path)

    if "logging" in config:
        log_config = config["logging"]
        level = log_config.get("logging_level", "info")
        logger = get_logger(__name__, level=level, set_global=True)
    else:
        logger = get_logger(__name__, level="info", set_global=True)

    from components.messenger import *
    from components.client import *
    from common.Report import ReportManager
    import _thread
    import time

    # set up report manager
    if "logging" in config:
        report_period = config.logging.get("report_period", 60)
        report_level = config.logging.get("report_level", "info")
        rm = ReportManager(report_period, report_level)
    else:
        rm = ReportManager()

    if "room_corner" in config:
        corner_list = []
        for corner in config["room_corner"]:
            corner_list.append(Point3D(corner))
        GV.register("room.corner", corner_list)

    if "client" in config:
        # Initialize communication manager to receive massage from ActiveMQ.
        GV.register("stomp_manager", CM())

        # Initialize messengers
        for key in config["client"]:
            messenger_cls = GV.get_messenger_class(key)
            assert messenger_cls is not None, f"Messenger name {key} not defined. Please register it before using."
            messenger = messenger_cls(config["client"][key])
            GV.register(f"messenger.{key}", messenger)
            if config["client"][key].get("report", True):
                rm.register(messenger)
        # Start messengers
        for key in config["client"]:
            _thread.start_new_thread(GV.get(f"messenger.{key}").start, ())

    rm.start()

    while not GV.get("ended", False):
        time.sleep(2)
    for key in config["client"]:
        GV.get(f"messenger.{key}").stop()
    rm.stop()
