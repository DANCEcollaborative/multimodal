import numpy as np


class Point2D():
    def __init__(self, x, y=None):
        if y is None:
            if type(x) == Point2D:
                self.x = x.x
                self.y = x.y
            else:
                self.x = float(x[0])
                self.y = float(x[1])
        else:
            self.x = float(x)
            self.y = float(y)

    def to_vec(self):
        return [self.x, self.y]

    def normalize(self):
        l = (self.x * self.x + self.y * self.y) ** (1/2)
        return Point2D(
            self.x / l,
            self.y / l
        )

    def __add__(self, other):
        return Point2D(
            self.x + other.x,
            self.y + other.y
        )

    def __sub__(self, other):
        return Point2D(
            self.x + other.x,
            self.y + other.y
        )

    def __mul__(self, other):
        return Point2D(
            self.x * other,
            self.y * other
        )

    def __rmul__(self, other):
        return Point2D(
            self.x * other,
            self.y * other
        )

    def __truediv__(self, other):
        return Point2D(
            self.x / other,
            self.y / other
        )

    def __abs__(self):
        return Point2D(
            abs(self.x),
            abs(self.y)
        )

    def __eq__(self, other):
        return self.x == other.x and self.y == other.y

    def __str__(self):
        return f"Point2D:({self.x}, {self.y})"


class Point3D():
    def __init__(self, x, y=None, z=None):
        if y is None or z is None:
            if type(x) == Point3D:
                self.x = x.x
                self.y = x.y
                self.z = x.z
            else:
                self.x = float(x[0])
                self.y = float(x[1])
                self.z = float(x[2])
        else:
            self.x = float(x)
            self.y = float(y)
            self.z = float(z)

    def to_vec(self):
        return [self.x, self.y, self.z]

    def normalize(self):
        l = p_len(self)
        return Point3D(
            self.x / l,
            self.y / l,
            self.z / l
        )

    def __add__(self, other):
        return Point3D(
            self.x + other.x,
            self.y + other.y,
            self.z + other.z
        )

    def __sub__(self, other):
        return Point3D(
            self.x + other.x,
            self.y + other.y,
            self.z + other.z
        )

    def __mul__(self, other):
        return Point3D(
            self.x * other,
            self.y * other,
            self.z * other
        )

    def __rmul__(self, other):
        return Point3D(
            self.x * other,
            self.y * other,
            self.z * other
        )

    def __truediv__(self, other):
        return Point3D(
            self.x / other,
            self.y / other,
            self.z / other
        )

    def __abs__(self):
        return Point3D(
            abs(self.x),
            abs(self.y),
            abs(self.z)
        )

    def __eq__(self, other):
        return self.x == other.x and self.y == other.y and self.z == other.z

    def __str__(self):
        return f"Point3D:({self.x}, {self.y}, {self.z})"


class Line3D():
    def __init__(self, p_0: Point3D, t: Point3D):
        if type(p_0) == Point3D:
            self.p_0 = p_0
        else:
            self.p_0 = Point3D(p_0)
        if type(t) == Point3D:
            self.t = t
        else:
            self.t = Point3D(t)

    def find_point_by_lambda(self, lam: float) -> Point3D:
        return self.p_0 + lam * self.t

    def find_point_by_x(self, x: float) -> Point3D:
        assert self.t.x != 0, "Can't find point by x for a line vertical to x axis!"
        lam = (x - self.p_0.x) / self.t.x
        return self.find_point_by_lambda(lam)

    def find_point_by_y(self, y: float) -> Point3D:
        assert self.t.y != 0, "Can't find point by y for a line vertical to y axis!"
        lam = (y - self.p_0.y) / self.t.y
        return self.find_point_by_lambda(lam)

    def find_point_by_z(self, z: float) -> Point3D:
        assert self.t.z != 0, "Can't find point by z for a line vertical to z axis!"
        lam = (z - self.p_0.z) / self.t.z
        return self.find_point_by_lambda(lam)

    def __str__(self):
        return f"Line3D:(\n" \
               f"   p_0: {self.p_0}, \n" \
               f"   t: {self.t}\n" \
               f")"


def p_zero() -> Point3D:
    return Point3D(0., 0., 0.)


def pp_distance(p1: Point3D, p2: Point3D) -> float:
    dx = p1.x - p2.x
    dy = p1.y - p2.y
    dz = p1.z - p2.z
    return (dx * dx + dy * dy + dz * dz) ** (1/2)


def p_len(p: Point3D) -> float:
    return pp_distance(p, p_zero())


def pp_dot(p1: Point3D, p2: Point3D) -> float:
    return p1.x * p2.x + p1.y * p2.y + p1.z * p2.z


def pp_cross(p1: Point3D, p2: Point3D) -> Point3D:
    return Point3D(
        p1.y * p2.z - p1.z * p2.y,
        p1.z * p2.x - p1.x * p2.z,
        p1.x * p2.y - p1.y * p2.x
    )


def pp_mid(p1: Point3D, p2: Point3D) -> Point3D:
    return Point3D(
        (p1.x + p2.x) / 2,
        (p1.y + p2.y) / 2,
        (p1.z + p2.z) / 2
    )


def ps_mid(pl) -> Point3D:
    x = 0.
    y = 0.
    z = 0.
    for p in pl:
        x += p.x
        y += p.y
        z += p.z
    num = len(pl)
    return Point3D(x / num, y / num, z / num)


def pp_cos(p1: Point3D, p2: Point3D) -> float:
    return pp_dot(p1, p2) / p_len(p1) / p_len(p2)


def pp_sin(p1: Point3D, p2: Point3D) -> float:
    return p_len(pp_cross(p1, p2)) / p_len(p1) / p_len(p2)


def pl_distance(p: Point3D, l: Line3D) -> float:
    return pp_distance(p, l.p_0) * pp_sin(p - l.p_0, l.t)


def p_is_zero(p: Point3D) -> bool:
    temp = abs(p)
    return temp.x + temp.y + temp.z < 1e-10


def pp_parallel(p1: Point3D, p2: Point3D) -> bool:
    return p1.x / p2.x == p1.y / p2.y and p1.y / p2.y == p1.z / p2.z


def ll_nearest(l1: Line3D, l2: Line3D) -> (Point3D, float):
    if pp_parallel(l1.t, l2.t):
        raise ArithmeticError("Parallel lines doesn't have a unique nearest point.")
    else:
        cross = pp_cross(l1.t, l2.t)
        dif = l1.p_0 - l2.p_0
        A = np.array(
            [[l1.t.x, -l2.t.x, cross.x],
             [l1.t.y, -l2.t.y, cross.y],
             [l1.t.z, -l2.t.z, cross.z]]
        )
        B = np.array(
            [[dif.x],
             [dif.y],
             [dif.z]]
        )
        X = np.linalg.solve(A, B)
        lamb_1, lamb_2, lamb_3 = X
        P3 = l1.p_0 + lamb_1 * l1.t
        P4 = l2.p_0 + lamb_2 * l2.t
        P = pp_mid(P3, P4)
        dis = pp_distance(P3, P4) / 2
        return P, dis


def is_zero(vec):
    return sum(abs(vec)) < 1e-6


def calc_position(start_points, directions, tolerance1=0.15, tolerance2=0.25):
    """
    Calculate the most possible positions for detected humans.
    :param
        start_points:
            A list of positions where the start point of lines are stored.
        lines:
            A list of line lists from different cameras.
            The length of the list should be the number of cameras.
            Every element in the list should be a list of lines
            which corresponds to people detected from a specific camera.
        tolerance1:
            Minimum error allowed to find a point given by two lines
        tolerance2:
            Minimum error allowed to treat two points as a same one
    :return: positions：
        Positions of people
    """

    # Construct lines
    lines = []
    for p0 in start_points:
        lines.append([])
        for t in directions:
            lines[-1].append(Line3D(p0, t))

    # Find nearest point candidate
    p0_num = len(start_points)
    point_candidate = []
    for i in range(p0_num):
        for j in range(i + 1, p0_num):
            for l1 in lines[i]:
                for l2 in lines[j]:
                    try:
                        p, dis = ll_nearest(lines[i])
                    except ArithmeticError as e:
                        print(e)
                        p = None
                        dis = None
                    if dis is not None and dis < tolerance1:
                        point_candidate.append(p)
    clustered_points = []
    for p0 in point_candidate:
        new_point = True
        for p in clustered_points:
            if pp_distance(p0, p[0]) < tolerance2:
                p.append(p0)
                new_point = False
                break
        if new_point:
            clustered_points.append([p0])
    res = []
    for cluster in clustered_points:
        if len(cluster) >= 2:
            res.append(ps_mid(cluster).to_vec())
    return res
