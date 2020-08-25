# TODO:
# The path to the OpenPose library should be a program parameter passed by user.
# Here we simply use the fixed path for test.

import sys
import os
import cv2
import numpy as np

openpose_dir = "/usr0/home/yansenwa/smartlab_component/openpose/"

try:
    sys.path.append(os.path.join(openpose_dir, 'build/python'))
    from openpose import pyopenpose as op
except ImportError as e:
    print("Error: OpenPose library could not be found. "
          "Did you enable `BUILD_PYTHON` in CMake and have this Python script in the right folder?")
    raise e


class OpenPoseUtil():
    def __init__(self):
        # TODO:
        # The model
        params = dict()
        params["model_folder"] = os.path.join(openpose_dir, "models")

        self.opWrapper = op.WrapperPython()
        self.opWrapper.configure(params)
        self.opWrapper.start()

    def find_pose(self, img):
        datum = op.Datum()
#        print(img)
#        print(img.shape)
        # TODO:
        # This is a very weird problem.
        # If you directly process on the data, it will fail.
        # But if you first write the data to a file and load the data again, it will be good.
        # I have no idea why this happens.
        cv2.imwrite("temp.png", img)
        imageToProcess = cv2.imread("temp.png")
        if imageToProcess is None:
            return np.array([]), None
        datum.cvInputData = imageToProcess
        self.opWrapper.emplaceAndPop([datum])
        return datum.poseKeypoints, datum.cvOutputData
