import numpy as np
from PIL import Image

REGION_COLORS = [
    (180, 200, 140), (200, 180, 120), (140, 200, 180),
    (200, 160, 100), (160, 140, 200), (140, 180, 220),
    (180, 140, 160), (160, 180, 140), (200, 180, 160),
]

def region_color(idx: int) -> tuple:
    return REGION_COLORS[idx % len(REGION_COLORS)]

def noise_tile(size: int, seed: int = 0) -> Image:
    rng = np.random.RandomState(seed)
    arr = (rng.rand(size, size) * 20 - 10).astype(np.int8)
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    for y in range(size):
        for x in range(size):
            v = 128 + arr[y, x]
            px[x, y] = (v, v, v, 30)
    return img
