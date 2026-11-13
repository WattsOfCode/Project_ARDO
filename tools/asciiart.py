"""Brings a piece of ARDO ASCII art up to the house standard (see Content/ASCII/README.md).

    python tools/asciiart.py clean Content/ASCII/Enemies/goblin.txt [more files...]
    python tools/asciiart.py check Content/ASCII

clean: blanks the background (the fill that reaches the edges, and the specks
left floating in it), trims empty margins, and writes the file back with CRLF.
check: lists any art that breaks the standard. Plain Python, nothing to install.
"""
import os
import sys
from collections import Counter, deque

RAMP = " .:-=+*#%@"
MAX_COLS = 300
MAX_PORTRAIT_ROWS = 260
SPECK_CELLS = 12


def read(path):
    with open(path, encoding="utf-8") as f:
        lines = f.read().replace("\r", "").split("\n")
    while lines and not lines[-1].strip():
        lines.pop()
    width = max((len(l) for l in lines), default=0)
    return [list(l.ljust(width)) for l in lines]


def write(path, grid):
    lines = ["".join(row).rstrip() for row in grid]
    while lines and not lines[0]:
        lines.pop(0)
    while lines and not lines[-1]:
        lines.pop()
    # Trim the blank columns on the left that every line shares.
    indent = min((len(l) - len(l.lstrip(" ")) for l in lines if l), default=0)
    lines = [l[indent:] for l in lines]
    with open(path, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("\n".join(lines) + "\n")


def tone(ch):
    return RAMP.index(ch) if ch in RAMP else len(RAMP) - 1


def border_cells(h, w):
    for x in range(w):
        yield 0, x
        yield h - 1, x
    for y in range(h):
        yield y, 0
        yield y, w - 1


def edge_fill(grid):
    h, w = len(grid), len(grid[0])
    edge = [grid[y][x] for y, x in border_cells(h, w)]
    return sum(ch != " " for ch in edge) / len(edge)


def clean(grid):
    h = len(grid)
    w = len(grid[0]) if h else 0
    if not h or not w or edge_fill(grid) <= 0.4:
        return grid  # already on a blank background

    # The backdrop is whatever fills the edges; anything no brighter than it, reached from an edge, is background.
    # If that still leaves the edges filled (a backdrop of mixed "." and ":"), allow one shade brighter.
    backdrop = Counter(grid[y][x] for y, x in border_cells(h, w)).most_common(1)[0][0]
    result = blank_background(grid, tone(backdrop))
    if edge_fill(result) > 0.4:
        result = blank_background(grid, tone(backdrop) + 1)
    return result


def blank_background(grid, limit):
    h, w = len(grid), len(grid[0])
    background = [[False] * w for _ in range(h)]
    queue = deque()
    for y, x in border_cells(h, w):
        if not background[y][x] and tone(grid[y][x]) <= limit:
            background[y][x] = True
            queue.append((y, x))
    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and not background[ny][nx] and tone(grid[ny][nx]) <= limit:
                background[ny][nx] = True
                queue.append((ny, nx))

    # Small islands floating in the background are noise from the old backdrop, not part of the figure.
    seen = [[False] * w for _ in range(h)]
    for sy in range(h):
        for sx in range(w):
            if background[sy][sx] or seen[sy][sx]:
                continue
            island = [(sy, sx)]
            seen[sy][sx] = True
            i = 0
            while i < len(island):
                y, x = island[i]
                i += 1
                for ny in (y - 1, y, y + 1):
                    for nx in (x - 1, x, x + 1):
                        if 0 <= ny < h and 0 <= nx < w and not background[ny][nx] and not seen[ny][nx]:
                            seen[ny][nx] = True
                            island.append((ny, nx))
            if len(island) < SPECK_CELLS:
                for y, x in island:
                    background[y][x] = True

    return [[" " if background[y][x] else grid[y][x] for x in range(w)] for y in range(h)]


def problems(path):
    grid = read(path)
    issues = []
    if not grid:
        return ["empty"]
    h, w = len(grid), len(grid[0])
    if w > MAX_COLS:
        issues.append(f"{w} columns (max {MAX_COLS})")
    if "Gear" not in path and h > MAX_PORTRAIT_ROWS:
        issues.append(f"{h} rows (max {MAX_PORTRAIT_ROWS})")
    stray = {ch for row in grid for ch in row if ch not in RAMP}
    if stray and "Gear" not in path:
        issues.append("characters outside the ramp: " + "".join(sorted(stray)))
    if "Gear" in path:
        return issues
    return issues


def notes(path):
    """Things worth knowing that are not wrong: art supplied with its own background is used as given."""
    grid = read(path)
    if "Gear" in path or not grid:
        return []
    filled = edge_fill(grid)
    return [f"keeps its own background ({filled:.0%} of the edge is filled)"] if filled > 0.4 else []


def main(argv):
    if len(argv) < 2 or argv[0] not in ("clean", "check"):
        print(__doc__)
        return 1

    if argv[0] == "clean":
        for path in argv[1:]:
            write(path, clean(read(path)))
            print("cleaned", path)
        return 0

    bad = 0
    for folder, _, files in os.walk(argv[1]):
        for name in sorted(files):
            if not name.endswith(".txt"):
                continue
            path = os.path.join(folder, name)
            for issue in problems(path):
                print(f"{path}: {issue}")
                bad += 1
            for note in notes(path):
                print(f"{path}: note: {note}")
    print("all art meets the standard" if bad == 0 else f"{bad} problem(s)")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
