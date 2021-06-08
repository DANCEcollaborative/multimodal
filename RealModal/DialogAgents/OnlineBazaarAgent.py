
from utils.GlobalVariables import GlobalVariables as GV
from DialogAgents.BaseAgent import BaseAgent
from utils.LoggingUtil import logging

from selenium import webdriver
import selenium.common.exceptions

import _thread
import time


class OnlineBazaarAgent(BaseAgent):
    def __init__(self, *args, **kwargs):
        super(OnlineBazaarAgent, self).__init__(self, args, kwargs)
        self.attached_listener = kwargs["Listener"]
        self.bazaar_url = kwargs["url"]
        option = webdriver.ChromeOptions()
        if not ("GUI" in kwargs and kwargs["GUI"]):
            option.add_argument("--headless")
        self.driver = webdriver.Chrome(chrome_options=option)
        self.driver.get(self.bazaar_url)
        self.refresh_time = 0.5
        if "refresh_time" in kwargs:
            self.refresh_time = kwargs["refresh_time"]
        self.agent_name = "AgentRachel"
        if "agent_name" in kwargs:
            self.agent_name = kwargs["agent_name"]

        self.last_time = ""
        self.running = True
        _thread.start_new_thread(self.listen_to_bazaar, ())

    def respond(self, s: str):
        self.driver.execute_script(f'socket.emit("sendchat", "{s}")')
        return ""

    def listen_to_bazaar(self):
        while self.running:
            time.sleep(self.refresh_time)
            try:
                elements = self.driver.find_elements_by_class_name("message_line")
            except selenium.common.exceptions.InvalidSelectorException:
                # print("Can't find message_line class, refreshing.")
                continue
            # print(f"found elements, length: {len(elements)}")
            for i in range(len(elements) - 1, -1, -1):
                element = elements[i]
                if element.find_element_by_class_name("user").text == self.agent_name:
                    # print(element.find_element_by_class_name("date").text)
                    try:
                        message = element.find_element_by_class_name("message")
                        timestamp = element.find_element_by_class_name("date").text
                    except (selenium.common.exceptions.InvalidSelectorException,
                            selenium.common.exceptions.NoSuchElementException):
                        continue
                    if timestamp == self.last_time:
                        break
                    else:
                        message = message.text
                        self.last_time = timestamp
                        self.attached_listener.send_text(message)
                        break

    def stop(self):
        self.running = False
