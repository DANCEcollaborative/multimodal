from common.Configuration import load_yaml

from common.logprint import get_logger


if __name__ == "__main__":
    # Get configurations
    # TODO: add support for argparse
    config_path = "config/config.yaml"
    config_all = load_yaml(config_path)

    if "logging" in config_all:
        log_config = config_all["logging"]
        level = log_config.get("logging_level", "info")
        logger = get_logger(__name__, level=level, set_global=True)
    else:
        logger = get_logger(__name__, level="info", set_global=True)

    from Socket.Server import *
    from components.server import *
    from common.Camera import *
    from common.Report import ReportManager

    # set up report manager
    if "logging" in config_all:
        report_period = config_all.logging.get("report_period", 60)
        report_level = config_all.logging.get("report_level", "info")
        rm = ReportManager(report_period, report_level)
    else:
        rm = ReportManager()

    if "camera" in config_all:
        config = config_all.camera
        # Register cameras in global variables:
        for key in config:
            prop = config[key]
            camera_cls = GV.get_camera_class(prop["type"])
            assert camera_cls is not None, f"Camera name {key} not defined. Please register it before using."
            GV.register(f"camera.{key}", camera_cls(prop))

    if "server" in config_all:
        config = config_all.server
        # Initialize modules:
        for key in config.processors:
            processor_cls = GV.get_processor_class(key)
            assert processor_cls is not None, f"Processor name {key} not defined. Please register it before using."
            processor_cls.initialize(config.processors[key].get("init_param", None))
            if GV.get(f"processor.entity", None) is None:
                GV.register(f"processor.entity", [])
                GV.register(f"processor.state", [])
                GV.register(f"processor.lock", [])
            GV.get("processor.entity").append(processor_cls(config.processors[key]))
            GV.get("processor.state").append("Available")
            GV.get("processor.lock").append(threading.Lock())

        # Build server:
        # Server to receive data from client:
        addr_in = (config.address.ip, config.address.port.upstream)
        recv_server_cls = GV.get_handler_class(config.recv_server)
        assert recv_server_cls is not None,\
            f"Handler name {config.recv_server} not defined. Please register it before using."
        recv_server = Server(addr_in, recv_server_cls)
        # Server to send data to client:
        addr_out = (config.address.ip, config.address.port.downstream)
        send_server_cls = GV.get_handler_class(config.send_server)
        assert send_server_cls is not None,\
            f"Handler name {config.send_server} not defined. Please register it before using."
        send_server = Server(addr_out, send_server_cls)

        recv_server.start()
        send_server.start()
