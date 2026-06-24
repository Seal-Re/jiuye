from PIL import Image, ImageDraw
from map_renderer.interfaces import IAssetProvider
from map_renderer.terrain_palette import region_color, noise_tile

class ProgrammaticIconProvider(IAssetProvider):
    def get_site_icon(self, kind: str, size: int) -> Image:
        img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        if kind == "normal":  _draw_house(d, size)
        elif kind == "resource": _draw_ore(d, size)
        elif kind == "secret":   _draw_mystery(d, size)
        elif kind == "sect":     _draw_tower(d, size)
        else: d.rectangle([2, 2, size-3, size-3], fill=(128, 128, 128, 200))
        return img

    def get_region_tile(self, biome_idx: int, size: int) -> Image:
        color = region_color(biome_idx)
        tile = Image.new("RGBA", (size, size), color + (255,))
        noise = noise_tile(size, seed=biome_idx)
        return Image.alpha_composite(tile, noise)

    def get_palette(self) -> dict:
        return {"normal": (139, 119, 80), "resource": (218, 165, 32),
                "secret": (148, 0, 211), "sect": (255, 215, 0)}

def _draw_house(d, s):
    m = s // 2
    d.polygon([(m, 4), (4, s // 3), (s - 5, s // 3)], fill=(139, 119, 80, 255))
    d.rectangle([s // 4, s // 3, s - s // 4, s - 4], fill=(160, 140, 100, 255))

def _draw_ore(d, s):
    m = s // 2
    d.polygon([(m, 4), (s - 5, m), (m, s - 5), (4, m)], fill=(218, 165, 32, 255))
    d.ellipse([m - 6, m - 6, m + 6, m + 6], fill=(255, 255, 200, 200))

def _draw_mystery(d, s):
    d.ellipse([4, 4, s - 5, s - 5], outline=(148, 0, 211, 255), width=2)
    d.text((s // 3, s // 4), "?", fill=(148, 0, 211, 255))

def _draw_tower(d, s):
    for tier in range(3):
        y = s - 10 - tier * 14
        w = 8 + tier * 6
        d.rectangle([s // 2 - w, y - 14, s // 2 + w, y], fill=(255, 200, 50, 255))
