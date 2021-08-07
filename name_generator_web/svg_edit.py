import re
# Gross regex shoggoth incoming...
circle_finder_regex = r"(M \d+(?:\.\d+)? \d+(?:\.\d+)?(?: A(?: \d+(?:\.\d+)?){7}){4})"

def read(path : str, mode : str = "r", **kwargs : str) -> str :
    if not kwargs and mode == "r":
        kwargs = { "encoding" : "utf-8"}
    with open(path, mode, **kwargs) as f:
        return f.read()

def write(path: str, content : str, mode : str = "w", **kwargs : str) -> int:
    if not kwargs and mode == "w":
        kwargs = { "encoding" : "utf-8" }
    with open(path, mode) as f:
        return f.write(content)

if __name__ == "__main__":
    radius_by_index = [
        2.0, 2.0, 1.0, 2.0, 2.0, 1.0, # first die
        2.0, 2.0, 2.0, 2.0, 1.0, 1.0 # second die
    ]
    svg_string = read("icon_vector.svg")
    pieces = re.split(circle_finder_regex, svg_string)
    matches = []
    for i in range(12):
        circle_index = 2*i + 1
        circle_string = pieces[circle_index]
        circle_tokens = circle_string.split()
        t = circle_tokens
        actual_radius = float(t[4])
        corners_as_string_tuples = [
            (t[1], t[2]),
            (t[9], t[10]),
            (t[17], t[18]),
            (t[25], t[26]),
        ]

        corners = [(float(x),float(y)) for (x,y) in corners_as_string_tuples]
        modified_corners = []
        center_x = sum(x for x,_ in corners) / 4
        center_y = sum(y for _,y in corners) / 4
        desired_radius = radius_by_index[i]
        for x,y in corners:
            delta_x = x - center_x
            delta_y = y - center_y
            scale_factor = desired_radius / actual_radius
            new_x = center_x + scale_factor*delta_x
            new_y = center_y + scale_factor*delta_y
            modified_corners.append((new_x, new_y))
        new_corner_strings = [
            f"{x:.8g} {y:.8g}" for x,y in modified_corners
        ]
        c = new_corner_strings
        r = desired_radius

        replacement_string = (
            f"M {c[0]}"
            f" A {r} {r} 0 0 0 {c[1]}"
            f" A {r} {r} 0 0 0 {c[2]}"
            f" A {r} {r} 0 0 0 {c[3]}"
            f" A {r} {r} 0 0 0 {c[0]}"
        )
        pieces[circle_index] = replacement_string
    new_svg_string = "".join(pieces)
    write("output.svg", new_svg_string)
