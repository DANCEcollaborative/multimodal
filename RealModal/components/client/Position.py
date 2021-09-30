from common.GlobalVariables import GV
from components.client.RemoteListener import BaseRemoteListener
import cv2

from Socket.Client import DataTransmissionClient as DTC
from common.logprint import get_logger
from common.Geometry import *
import time

logger = get_logger(__name__)

@GV.register_listener("position")
class PositionDisplayListener(BaseRemoteListener):
    def __init__(self, config):
        super(PositionDisplayListener, self).__init__(config)
        self.display_size = config.get("display_size", (500, 500))
        self.display_margin = config.get("display_margin", 50)
        self.topic_to_psi = config.get("topic_to_psi", "Python_PSI_Location")
        self.corners = []
        for corner in config.get("room_corner", []):
            self.corners.append(Point3D(corner))

        self.num_received = 0
        self.total_latency = 0.

    def receive(self, socket: DTC):
        self.num_received += 1
        logger.debug("In position display listener... receiving")
        prop_dict = self.receive_properties(socket)
        pos_num = socket.recv_int()
        logger.debug("%d position(s) to receive." % pos_num)
        to_send = str(pos_num) + ';' + str(prop_dict["timestamp"])
        time_all = GV.get("visualizer.sendtime", dict())
        logger.debug(f"{len(time_all)}, {time_all.keys()}, {prop_dict['timestamp']}")
        logger.debug(prop_dict['timestamp'])
        if str(prop_dict['timestamp']) in time_all:
            logger.debug(f"For frame {prop_dict['timestamp']}, processing time: {time.time() - time_all[str(prop_dict['timestamp'])]}")
            self.total_latency += time.time() - time_all[str(prop_dict['timestamp'])]
            current_ts = prop_dict['timestamp']
            keys = time_all.keys()
            for key in list(keys):
                if int(key) <= current_ts:
                    GV.unregister(f"visualizer.sendtime.{key}")
        raw_info = []
        person = []
        for i in range(pos_num):
            logger.debug("receiving person %d" % i)
            x0 = socket.recv_float()
            y0 = socket.recv_float()
            x = socket.recv_float()
            y = socket.recv_float()
            z = socket.recv_float()
            c = socket.recv_str()
            raw_info.append((x0, y0, x, y, z, c))
            logger.debug(f"received person {i}, location: ({x0}, {y0})")
        for i, (x0, y0, x, y, z, c) in enumerate(raw_info):
            location_querier = GV.get("messenger.location_querier", None)
            if location_querier is not None:
                result = location_querier.query(None, prop_dict["timestamp"], Point2D(x0, y0))
                # result = camera[cid].camera_to_real
                if p_is_zero(result):
                    person.append((x, y, z))
                else:
                    person.append((result.x, result.y, result.z))
                to_send += f";person_{c}&{result.x}:{result.y}:{result.z}"
            else:
                person.append((x, y, z))
                to_send += f";person_{c}&{x}:{y}:{z}"
        logger.debug(to_send)
        self.update_layout_image(self.corners, person)
        GV.get("stomp_manager").send(self.topic_to_psi, to_send)
        return []

    def draw(self, img, buf):
        return img

    def draw_layout(self, corner, person):
        minx = min(map(lambda x: x.x if type(x) is Point3D else x[0], corner))
        miny = min(map(lambda x: x.y if type(x) is Point3D else x[1], corner))
        maxx = max(map(lambda x: x.x if type(x) is Point3D else x[0], corner))
        maxy = max(map(lambda x: x.y if type(x) is Point3D else x[1], corner))
        dis_x, dis_y = self.display_size
        mar = self.display_margin
        logger.debug(person)

        def cov(x, y=None, z=None) -> (int, int):
            if type(x) == int:
                nx = int(mar + (x - minx) * (dis_x - 2 * mar) / (maxx - minx))
                ny = int(mar + (y - miny) * (dis_y - 2 * mar) / (maxy - miny))
            elif type(x) == Point3D:
                nx = int(mar + (x.x - minx) * (dis_x - 2 * mar) / (maxx - minx))
                ny = int(mar + (x.y - miny) * (dis_y - 2 * mar) / (maxy - miny))
            else:
                nx = int(mar + (x[0] - minx) * (dis_x - 2 * mar) / (maxx - minx))
                ny = int(mar + (x[1] - miny) * (dis_y - 2 * mar) / (maxy - miny))
            return ny, nx

        ret = np.ones((dis_x, dis_y, 3), dtype=np.uint8) * 240
        for i in range(len(corner)):
            cv2.line(ret, cov(corner[i]), cov(corner[(i + 1) % len(corner)]), (0, 0, 0), 2)
        for p in person:
            cv2.circle(ret, cov(p), 5, (0, 0, 255), cv2.FILLED)
        return ret

    def update_layout_image(self, corner, person):
        img = self.draw_layout(corner, person)
        cv2.imshow("Smart Room - Body Positions", img)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            return

    def on_report_overall(self, overall_time, logger):
        logger.info(f"Position module received {self.num_received} messages.")
        logger.info(f"Average latency: {0.0 if self.num_received == 0 else self.total_latency / self.num_received}")