from utils.PositionCalcUtil import *
import math


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
