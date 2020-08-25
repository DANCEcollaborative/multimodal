import math

COLOR_RANGE = {
    'Black': [((0, 0, 0), (180, 255, 46))],
    'Gray': [((0, 0, 46), (180, 43, 220))],
    'White': [((0, 0, 220), (180, 43, 255))],
    'Red': [((0, 43, 46), (10, 255, 255)),
            ((155, 43, 46), (180, 255, 255))],
    'Orange': [((10, 43, 46), (25, 255, 255))],
    'Yellow': [((25, 43, 46), (34, 255, 255))],
    'Green': [((34, 43, 46), (77, 255, 255))],
    'Cyan': [((77, 43, 46), (99, 255, 255))],
    'Blue': [((99, 43, 46), (124, 255, 255))],
    'Purple': [((124, 43, 46), (155, 255, 255))],
}


def bgr2hsv(b: float, g: float, r: float):
    b /= 255.
    g /= 255.
    r /= 255.
    bgr_max = float(max(b, g, r))
    bgr_min = float(min(b, g, r))
    v = bgr_max
    if v == 0.:
        s = 0.
    else:
        s = (v - bgr_min) / v
    if bgr_max == bgr_min:
        h = 0
    elif v == r:
        h = 60 * (g - b) / (v - bgr_min)
    elif v == g:
        h = 120 + 60 * (b - r) / (v - bgr_min)
    elif v == b:
        h = 240 + 60 * (r - g) / (v - bgr_min)
    else:
        print("bgr_max:", bgr_max)
        print("bgr_min:", bgr_min)
        print("v:", v)
        print("r:", r)
        print("g:", g)
        print("b:", b)
        assert False, "Your computer is broken."
    if h < 0:
        h = h + 360
    assert 0 <= v <= 1 and 0 <= s <= 1 and 0 <= h <= 360, f"Conversion Error happens for ({b}, {g}, {r}) -> ({h}, {s}, {v})"
    h = h / 2
    s = s * 255
    v = v * 255
    return h, s, v


def get_color_name(b: float or tuple, g: float = None, r: float = None):
    if g is None:
        b, g, r = b
    if math.isnan(b) or math.isnan(g) or math.isnan(r):
        return "Unknown"
    h, s, v = bgr2hsv(b, g, r)
    return get_color_name_by_hsv(h, s, v)


def get_color_name_by_hsv(h: float, s: float, v: float):
    for color_name in COLOR_RANGE:
        range_list = COLOR_RANGE[color_name]
        for color_range in range_list:
            color_min, color_max = color_range
            h_min, s_min, v_min = color_min
            h_max, s_max, v_max = color_max
            if h_min <= h <= h_max and s_min <= s <= s_max and v_min <= v <= v_max:
                return color_name
    print("h: ", h)
    print("s: ", s)
    print("v: ", v)
    raise ValueError("Undefined Color")
