from utils.PositionCalcUtil import *
import math
import abc


class CameraBase(metaclass=abc.ABCMeta):
    def __init__(self,
                 pos_camera: Point3D,
                 dir_camera: Point3D,
                 dir_x: Point3D,
                 theta: float = None,
                 phi: float = None,
                 whratio: float = None):
        self.pos_camera = pos_camera
        self.dir_z = self.dir_camera = dir_camera
        self.dir_x = dir_x
        self.theta = theta
        self.phi = phi
        self.whratio = whratio

    @abc.abstractmethod
    def image_mapping(self, pic: Point2D) -> Line3D:
        pass

    @abc.abstractmethod
    def world_mapping(self, coord: Point3D) -> Point3D:
        pass


class WebCamera(CameraBase):
    def __init__(self,
                 pos_camera: Point3D,
                 dir_camera: Point3D,
                 dir_x: Point3D,
                 theta: float = None,
                 phi: float = None,
                 whratio: float = None):
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
        num_none = 0
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
        super(WebCamera, self).__init__(pos_camera, dir_z, dir_x, theta, phi, whratio)

    def image_mapping(self, pic: Point2D) -> Line3D:
        """
        Define a mapping function for webcam.
        :param pic: point of a pixel.
        :return: a line in the real world corresponding to the pixel.
        """
        p_0 = Point3D(self.pos_camera)
        dir_y = pp_cross(self.dir_z, self.dir_x)
        if self.theta is not None:
            delta_ey = 1 * math.tan(self.theta / 2) * 2 * dir_y
            if self.phi is not None:
                delta_ex = 1 * math.tan(self.phi / 2) * 2 * self.dir_x
            else:
                delta_ex = 1 * math.tan(self.theta / 2) * 2 * self.whratio * self.dir_x
            # In image space, y increases from top to bottom, but that will be a left-hand coordination system.
            # We intentionally inverse delta_ey here to transform it to real word coordination system.
        else:
            delta_ex = 1 * math.tan(self.phi / 2) * 2 * self.dir_x
            delta_ey = 1 * math.tan(self.phi / 2) * 2 / self.whratio * dir_y
        ret_dir = self.dir_z + delta_ex * (0.5 - pic.x) + delta_ey * (0.5 - pic.y)
        return Line3D(p_0, ret_dir)

    def world_mapping(self, coord: Point3D) -> Point3D:
        dir_y = pp_cross(self.dir_z, self.dir_x)
        return coord.x * self.dir_x + coord.y * dir_y + coord.z * self.dir_z + self.pos_camera


def WebcamMapping(pos_camera: Point3D,
                  dir_camera: Point3D,
                  dir_x: Point3D,
                  theta: float,
                  phi: float = None,
                  whratio: float = None):
    """
    Generate a webcam mapping function given the parameters of a camera.
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

    # x axis must be vertical to the direction of the camera
    assert pp_dot(dir_camera, dir_x) < 1e-10, "dot(dir_camera, dir_x) != 0."
    assert phi is not None or whratio is not None, "At least one of phi or whratio should be provided."
    dir_x = dir_x.normalize()
    dir_z = dir_camera.normalize()

    def mapping(pic: Point2D) -> Line3D:
        """
        The returned mapping function.
        :param pic: the position of a pixel(normalized to [0, 1]).
        :return: the line in the real world.
        """
        p_0 = Point3D(pos_camera)
        dir_y = pp_cross(dir_z, dir_x)
        delta_ey = 1 * math.tan(theta / 2) * 2 * dir_y
        if phi is not None:
            delta_ex = 1 * math.tan(phi / 2) * 2 * dir_x
        else:
            delta_ex = 1 * math.tan(theta / 2) * 2 * whratio * dir_x
        # In image space, y increases from top to bottom, but that will be a left-hand coordination system.
        # We intentionally inverse delta_ey here to transform it to real word coordination system.
        ret_dir = dir_z + delta_ex * (0.5 - pic.x) + delta_ey * (0.5 - pic.y)

        return Line3D(p_0, ret_dir)

    return mapping
