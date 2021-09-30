from common.logprint import get_logger
import time
import _thread


class ReportCallback():
    def __init__(self):
        pass

    def on_register(self, manager):
        self._report_manager = manager

    def on_report_single_period(self, period_time, logger):
        pass

    def on_report_overall(self, overall_time, logger):
        pass


class ReportManager():
    def __init__(self, report_period=30, report_level="info"):
        self.report_dict = {}
        self.logger = get_logger("Report", report_level)
        self.period = report_period
        self.running = False
        self.start_time = 0
        self.last_time = 0

    def register(self, obj: ReportCallback):
        obj.on_register(self)
        self.report_dict[id(obj)] = obj

    def start(self):
        self.running = True
        self.last_time = self.start_time = time.time()
        self.logger.info("Realmodal starts.")
        self.logger.info(f"Number of components registered: {len(self.report_dict)}")
        _thread.start_new_thread(self.timed_report, ())

    def stop(self):
        self.running = False
        self.logger.info("Realmodal stops.")

    def timed_report(self):
        while self.running:
            if self.period < 0:
                self.logger.info("Running without reporting.")
                break
            if self.period > 0.5:
                time.sleep(self.period - 0.5)
            while time.time() - self.last_time < self.period:
                pass
            this_time = time.time()
            self.logger.info(f"###############################################")
            self.logger.info(f"Reporting:")
            period_time = this_time - self.last_time
            self.logger.info(f"In this period (lasting for {period_time:.2f} seconds), ")
            for key in self.report_dict:
                obj = self.report_dict[key]
                obj.on_report_single_period(period_time, self.logger)
            overall_time = this_time - self.start_time
            self.logger.info(f"###############################################")
            self.logger.info(f"Overall (lasting for {overall_time:.2f} seconds), ")
            for key in self.report_dict:
                obj = self.report_dict[key]
                obj.on_report_overall(overall_time, self.logger)
            self.last_time = this_time
            self.logger.info(f"###############################################")
