from Socket.BaseSocket import BaseTCPSocket
from utils.GlobalVaribles import GlobalVariables as GV

import abc
from Socket.Client import DataTransmissionClient as DTC
from utils.PositionCalcUtil import *
from utils.LoggingUtil import logging
import cv2
import numpy as np
import base64


class BaseRemoteListener(metaclass=abc.ABCMeta):
    def __init__(self, topic=None):
        self.topic = topic

    @staticmethod
    def receive_properties(socket: BaseTCPSocket) -> dict:
        prop_dict = dict()
        prop_str = socket.recv_str()
        while prop_str != "END":
            res = prop_str.split(":", 2)
            if len(res) == 3:
                prop_name, prop_type, prop_content = res
                if prop_type == "str":
                    prop_dict[prop_name] = str(prop_content)
                elif prop_type == "int":
                    prop_dict[prop_name] = int(prop_content)
                elif prop_type == "float":
                    prop_dict[prop_name] = float(prop_content)
            prop_str = socket.recv_str()
            print(prop_str)
        return prop_dict

    @abc.abstractmethod
    def receive(self, socket):
        pass

    @abc.abstractmethod
    def draw(self, img, buf):
        pass


class FaceRecognitionListener(BaseRemoteListener):
    def __init__(self, topic=None):
        super(FaceRecognitionListener, self).__init__(topic)

    def receive(self, socket: DTC):
        buf = []
        face_num = socket.recv_int()
        print("%d face(s) to receive." % face_num)
        for i in range(face_num):
            print("receiving face %d" % i)
            fid = socket.recv_int()
            loc1 = socket.recv_int()
            loc2 = socket.recv_int()
            loc3 = socket.recv_int()
            loc4 = socket.recv_int()
            buf.append((fid, (loc1, loc2, loc3, loc4)))
        logging(buf)
        return buf

    def draw(self, img, buf):
        # logging("Face Recognition drawing...")
        for fid, (top, right, bottom, left) in buf:
            cv2.rectangle(img, (left, top), (right, bottom), (0, 255, 0), 1)
            cv2.rectangle(img, (left, bottom - 25), (right, bottom), (0, 255, 0), cv2.FILLED)
            cv2.putText(img, "face %d" % fid, (left + 6, bottom - 6), cv2.FONT_HERSHEY_DUPLEX, 0.5, (255, 255, 255), 1)
        return img


class OpenPoseListener(BaseRemoteListener):
    def __init__(self, topic=None):
        super(OpenPoseListener, self).__init__(topic)

    def receive(self, socket: DTC):
        buf = []
        pose_num = socket.recv_int()
        print("%d pose(s) to receive." % pose_num)
        if pose_num > 0:
            data = socket.recv_data()
            decoded = base64.b64decode(data)
            array = np.fromstring(decoded, dtype=np.float32).reshape(pose_num, 25, 3)
            buf = array.tolist()
        print(buf)
        return buf

    def draw(self, img, buf):
        for person in buf:
            for point in person:
                x, y, c = point
                if c < 1e-3:
                    continue
                cv2.circle(img, (int(x), int(y)), 3, (0, 255, 0), 1)
        return img


class PositionDisplayListener(BaseRemoteListener):
    def __init__(self, topic=None):
        super(PositionDisplayListener, self).__init__(topic)

    def receive(self, socket: DTC):
        logging("In position display listener... receiving")
        prop_dict = self.receive_properties(socket)
        pos_num = socket.recv_int()
        print("%d position(s) to receive." % pos_num)
        to_send = str(pos_num) + ';' + str(prop_dict["timestamp"])
        raw_info = []
        person = []
        for i in range(pos_num):
            print("receiving person %d" % i)
            x0 = socket.recv_float()
            y0 = socket.recv_float()
            x = socket.recv_float()
            y = socket.recv_float()
            z = socket.recv_float()
            c = socket.recv_str()
            raw_info.append((x0, y0, x, y, z, c))
            print(f"received person {i}, location: ({x0}, {y0})")
        for i, (x0, y0, x, y, z, c) in enumerate(raw_info):
            if GV.UseDepthCamera:
                result = GV.LocationQuerier.query(None, prop_dict["timestamp"], Point2D(x0, y0))
                # result = camera[cid].camera_to_real
                if p_is_zero(result):
                    person.append((x, y, z))
                else:
                    person.append((result.x, result.y, result.z))
                to_send += f";person_{c}&{result.x}:{result.y}:{result.z}"
            else:
                person.append((x, y, z))
                to_send += f";person_{c}&{x}:{y}:{z}"
        logging(to_send)
        self.update_layout_image(GV.CornerPosition, person)
        GV.manager.send("Python_PSI_Location", to_send)
        return []

    def draw(self, img, buf):
        return img

    @staticmethod
    def draw_layout(corner, person):
        minx = min(map(lambda x: x[0], corner))
        miny = min(map(lambda x: x[1], corner))
        maxx = max(map(lambda x: x[0], corner))
        maxy = max(map(lambda x: x[1], corner))
        dis_x, dis_y = GV.display_size
        mar = GV.display_margin
        logging(person)

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
