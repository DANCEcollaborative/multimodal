from typing import Union, List
from common.GlobalVariables import GV
from common.Geometry import *
import math
import abc


class CameraBase(metaclass=abc.ABCMeta):
    def __init__(self, config):
        pass

    @abc.abstractmethod
    def image_mapping(self, pic: Point2D) -> Line3D:
        pass

    @abc.abstractmethod
    def world_mapping(self, coord: Point3D) -> Point3D:
        pass


@GV.register_camera("webcam")
class WebCamera(CameraBase):
    def __init__(self, config):
        """
        Define a webcam instance given the parameters of a camera.
        :param pos_camera: The position of the camera.
        :param dir_camera: The direction of the pixel center.
        :param dir_x: The direction of the x axis for camera.
        :param theta: The vertical span angle of the camera.
        :param phi: The horizontal span angle of the camera. If set None, inferred from the size of images.
        :param whratio:
            The width/height ratio of the input image(4:3 or 16:9)
            If phi is set None, this ratio will be used to infer horizontal span angle.
        :return: a mapping function from a pixel to a line in the real world.
        """
        super(WebCamera, self).__init__(config)
        pos_camera = config.pos_camera
        dir_camera = config.dir_camera
        dir_x = config.dir_x
        theta = config.get("theta", None)
        phi = config.get("phi", None)
        whratio = config.get("whratio", None)
        num_none = 0
        if pos_camera is not Point3D:
            pos_camera = Point3D(pos_camera)
        if dir_camera is not Point3D:
            dir_camera = Point3D(pos_camera)
        if dir_x is not Point3D:
            dir_x = Point3D(dir_x)
        if theta is None:
            num_none += 1
        if phi is None:
            num_none += 1
        if whratio is None:
            num_none += 1
        assert num_none < 2, "At least two of theta, phi, whratio should be provided for a webcam."
        assert pp_dot(dir_camera, dir_x) < 1e-4, "dot(dir_camera, dir_x) != 0."
        dir_x = dir_x.normalize()
        dir_z = dir_camera.normalize()
        self.pos_camera = pos_camera
        self.dir_z = self.dir_camera = dir_z
        self.dir_x = dir_x
        self.theta = theta
        self.phi = phi
        self.whratio = whratio
        self.dir_y = pp_cross(self.dir_z, self.dir_x)

    def image_mapping(self, pic: Point2D) -> Line3D:
        """
        Define a mapping function for webcam.
        :param pic: point of a pixel.
        :return: a line in the real world corresponding to the pixel.
        """
        p_0 = Point3D(self.pos_camera)
        if self.theta is not None:
            delta_ey = 1 * math.tan(self.theta / 2) * 2 * self.dir_y
            if self.phi is not None:
                delta_ex = 1 * math.tan(self.phi / 2) * 2 * self.dir_x
            else:
                delta_ex = 1 * math.tan(self.theta / 2) * 2 * self.whratio * self.dir_x
            # In image space, y increases from top to bottom, but that will be a left-hand coordination system.
            # We intentionally inverse delta_ey here to transform it to real word coordination system.
        else:
            delta_ex = 1 * math.tan(self.phi / 2) * 2 * self.dir_x
            delta_ey = 1 * math.tan(self.phi / 2) * 2 / self.whratio * self.dir_y
        ret_dir = self.dir_z + delta_ex * (0.5 - pic.x) + delta_ey * (0.5 - pic.y)
        return Line3D(p_0, ret_dir)

    def world_mapping(self, coord: Point3D) -> Point3D:
        """
        Define a mapping function from camera coordinates to world coordinates
        :param coord:
        :return:
        """
        return coord.x * self.dir_x + coord.y * self.dir_y + coord.z * self.dir_z + self.pos_camera
