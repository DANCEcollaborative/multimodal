from utils.GlobalVariables import GlobalVariables as GV
from utils.LoggingUtil import logging

import abc
import threading
import base64

from Socket.BaseSocket import BaseTCPSocket

from utils.FaceRecognitionUtil import FaceRecognitionUtil as FRU
from utils.OpenPoseUtil import OpenPoseUtil as OPU
from utils.PositionCalcUtil import *
from utils.ColorUtil import get_color_name

import numpy as np


def add_processor(processor):
    GV.Processor.append(processor)
    GV.ProcessorState.append("Available")
    GV.ProcessorLock.append(threading.Lock())


class BaseImageProcessor(metaclass=abc.ABCMeta):
    def __init__(self, topic=None):
        # TODO: auto synchronize the topic(may not be possible)
        self.topic = topic
        self.current = None

    def base_send(self, soc: BaseTCPSocket):
        soc.send_str(f"type:{self.topic}:{self.current['camera_id']}")
        try:
            self.send(soc)
        except Exception as e:
            print(e)

    @abc.abstractmethod
    def send(self, soc: BaseTCPSocket):
        pass

    def base_process(self, info, pos, ip_addr):
        if GV.ProcessorLock[pos].acquire(blocking=False):
            GV.ProcessorState[pos] = f"Processing:{ip_addr}"
            self.current = info.copy()
            try:
                self.process(info)
                GV.ProcessorState[pos] = f"Pending:{ip_addr}"
            except Exception as e:
                print(e)
                GV.ProcessorState[pos] = "Available"
            finally:
                GV.ProcessorLock[pos].release()

    @abc.abstractmethod
    def process(self, info):
        pass


class FaceRecognitionProcessor(BaseImageProcessor):
    def __init__(self, topic=None):
        super(FaceRecognitionProcessor, self).__init__(topic)
        assert GV.fru is not None, "Face Recognizer should be initialized before using!"
        self.recognizer = GV.fru
        self.face_id = []
        self.face_loc = []
        GV.locks["FaceRecognition"] = threading.Lock()

    def process(self, info):
        img = info['img']
        if img.shape[-1] == 4:
            img = img[:, :, :3]
        self.face_id, self.face_loc = self.recognizer.recognize(img)
        GV.locks["FaceRecognition"].acquire()
        GV.FaceRecognitionResult[info['camera_id']] = (self.face_id, self.face_loc)
        GV.locks["FaceRecognition"].release()

    def send(self, soc: BaseTCPSocket):
        l = len(self.face_id)
        print("find %d face(s) in the image" % l)
        soc.send_int(l)
        for i in range(l):
            print("sending face %d" % i)
            soc.send_int(self.face_id[i])
            soc.send_int(self.face_loc[i][0])
            soc.send_int(self.face_loc[i][1])
            soc.send_int(self.face_loc[i][2])
            soc.send_int(self.face_loc[i][3])


class OpenPoseProcessor(BaseImageProcessor):
    def __init__(self, topic=None):
        super(OpenPoseProcessor, self).__init__(topic)
        assert GV.opu is not None, "OpenPose should be initialized before using!"
        self.openpose = GV.opu
        self.poseKeypoints = None
        GV.locks["OpenPose"] = threading.Lock()

    def process(self, info):
        img = info['img']
        if img.shape[-1] == 4:
            img = img[:, :, :3]
        self.poseKeypoints, _ = self.openpose.find_pose(img)
        GV.locks["OpenPose"].acquire()
        GV.OpenPoseResult[info['camera_id']] = self.poseKeypoints.copy()
        GV.locks["OpenPose"].release()

    def send(self, soc: BaseTCPSocket):
        if len(self.poseKeypoints.shape) == 3:
            l = self.poseKeypoints.shape[0]
        else:
            l = 0
        print("find %d person(s) in the image" % l)
        soc.send_int(l)
        # Send poses only when there are at least one person detected.
        # This is because self.poseKeypoints will be a very weird value if no people is detected due to some features
        # or bugs in the  Openpose Library.
        if l > 0:
            soc.send_data(base64.b64encode(self.poseKeypoints.tostring()))


class PositionProcessor(BaseImageProcessor):
    def __init__(self, backend="Openpose", topic=None):
        super(PositionProcessor, self).__init__(topic)

        self.backendNotFound = False
        self.timestamp = None
        self.positions = []
        if backend.lower() not in ["openpose", "facerecognition"]:
            raise ValueError("Undefined backend.")
        self.backend = backend.lower()

    def process(self, info):
        self.timestamp = info["timestamp"]
        if self.backend == "openpose":
            self.process_openpose(info)
        elif self.backend == "facerecognition":
            self.process_face_rec(info)

    def process_face_rec(self, info):
        camera_ids = list(GV.CameraList.keys())
        # check whether face recognition is enabled
        if not GV.UseFaceRecognition:
            if not self.backendNotFound:
                self.backendNotFound = True
                raise RuntimeError("Face Recognition module not enabled.")
            return

        # check whether all cameras have updated results
        flag = False
        while not flag:
            flag = True
            for cid in camera_ids:
                flag = flag and (cid in GV.FaceRecognitionResult)
        GV.locks["FaceRecognition"].acquire()
        faces = GV.FaceRecognitionResult.copy()
        GV.locks["FaceRecognition"].release()

        if len(camera_ids) == 1:
            face_id, face_loc = faces[camera_ids[0]]
            self.positions = []
            for i, face in enumerate(face_loc):
                top, right, bottom, left = face
                x0, y0 = (right + left) / 2, (top + bottom) / 2
                h, w = info['img'].shape[:2]
                x0 = float(x0) / w
                y0 = float(y0) / h
                line_center = GV.CameraList[camera_ids[0]].image_mapping(Point2D(x0, y0))
                p_center = line_center.find_point_by_z(GV.SingleCameraDistance)
                self.positions.append((Point2D(x0, y0), p_center.to_vec()))
        else:
            # TODO: add position recognition when using several cameras.
            pass

    def process_openpose(self, info):
        # TODO: [URGENT!!] here, the info and GV.OpenPoseResult might not point to a same image.
        camera_ids = list(GV.CameraList.keys())
        # check whether face recognition is enabled
        if not GV.UseOpenpose:
            if not self.backendNotFound:
                self.backendNotFound = True
                raise RuntimeError("OpenPose module not enabled.")
            return

        # check whether all cameras have updated results
        flag = False
        while not flag:
            flag = True
            for cid in camera_ids:
                flag = flag and (cid in GV.OpenPoseResult)
        GV.locks["OpenPose"].acquire()
        keypoints = GV.OpenPoseResult.copy()
        GV.locks["OpenPose"].release()

        nose_index = 0
        neck_index = 1
        midhip_index = 8

        if len(camera_ids) == 1:
            self.positions = []
            cid = camera_ids[0]
            if len(keypoints[cid].shape) < 3:
                return
            for i, points in enumerate(keypoints[cid]):
                if not is_zero(points[neck_index]):
                    use_index = neck_index
                elif not is_zero(points[nose_index]):
                    use_index = nose_index
                elif not is_zero(points[midhip_index]):
                    use_index = midhip_index
                else:
                    continue
                x0, y0, _ = points[use_index]
                h, w = info['img'].shape[:2]

                # clothes color detection
                if is_zero(points[neck_index]):
                    cloth_color = "Unknown"
                else:
                    cropped_area = info['img'][int(y0)+5:int(y0)+15, int(x0)-5:int(x0)+5]
                    if sum(cropped_area.shape) > 0:
                        cropped_color = np.mean(cropped_area, (0, 1))
                        cloth_color = get_color_name(cropped_color)
                    else:
                        cloth_color = "Unknown"
                x0 = float(x0) / w
                y0 = float(y0) / h
                line_center = GV.CameraList[camera_ids[0]].image_mapping(Point2D(x0, y0))
                p_center = line_center.find_point_by_z(GV.SingleCameraDistance)
                self.positions.append(
                    (Point2D(x0, y0), GV.CameraList[cid].world_mapping(p_center).to_vec(), cloth_color)
                )
        else:
            # Determine which body key point is used to calculate positions
            num_nose = 0
            num_neck = 0
            num_midhip = 0
            for cid in camera_ids:
                for points in keypoints[cid]:
                    if not is_zero(points[nose_index]):
                        num_nose += 1
                    if not is_zero(points[neck_index]):
                        num_neck += 1
                    if not is_zero(points[midhip_index]):
                        num_midhip += 1
            use_index = 1
            if num_neck >= num_nose and num_neck >= num_midhip:
                use_index = neck_index
            elif num_nose >= num_midhip:
                use_index = nose_index
            else:
                use_index = midhip_index

            # Gather the line information
            start_point = []
            direction = []
            for cid in camera_ids:
                start_point.append(GV.CameraList[cid].pos_camera)
                direction.append([])
                for points in keypoints[cid]:
                    keypoint = points[use_index]
                    if not is_zero(keypoint):
                        direction[-1].append(GV.CameraList[cid].image_mapping(Point2D(keypoint[0], keypoint[1])))

            # Calculate position
            self.positions = calc_position(start_point, direction)

    def send(self, soc: BaseTCPSocket):
        soc.send_str(f"timestamp:int:{self.timestamp}")
        soc.send_str("END")
        l = len(self.positions)
        print("find %d person(s) in the space" % l)
        soc.send_int(l)
        for i, (p, (x, y, z), c) in enumerate(self.positions):
            print("sending person %d" % i)
            soc.send_float(p.x)
            soc.send_float(p.y)
            soc.send_float(x)
            soc.send_float(y)
            soc.send_float(z)
            soc.send_str(c)
