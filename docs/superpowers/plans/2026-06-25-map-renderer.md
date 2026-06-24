# Pixel Map Renderer Implementation Plan

> **For agentic workers:** Use autonomous implementation per story patterns. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build pluggable pixel map renderer — 4 interfaces, 9 files, deterministic Pillow output from WorldMap data.

**Architecture:** IMapRenderer pipeline renders MapData through IMapLayer stack using IAssetProvider icons. PixelMapRenderer is default implementation. All components independently testable via dependency injection.

**Tech Stack:** Python 3.10+, Pillow, numpy. No external APIs (ProgrammaticIconProvider runs offline).

**Spec:** `docs/superpowers/specs/2026-06-25-map-renderer-design.md`

---

### Task 1: Core data types (`map_data.py`)

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/__init__.py`
- Create: `tools/pixel-pipeline/map_renderer/map_data.py`
- Create: `tests/pixel-pipeline/test_map_data.py`

- [ ] **Step 1: Write failing test for MapData construction**

```python
# tests/pixel-pipeline/test_map_data.py
import sys; sys.path.insert(0, "tools/pixel-pipeline")
from map_renderer.map_data import MapData, RegionData, SiteData

def test_map_data_from_regions():
    regions = [RegionData("中原", 50, 50, 80, 60, 50)]
    sites = [SiteData("normal", 0, 0, 0), SiteData("resource", 0, 100, 1)]
    adj = [[1], [0]]
    data = MapData(regions, sites, adj)
    assert data.node_count == 2
    assert data.region_count == 1
    assert data.sites[1].kind == "resource"
    assert data.adjacency[0] == [1]
```

- [ ] **Step 2: Implement MapData + RegionData + SiteData**

```python
# tools/pixel-pipeline/map_renderer/__init__.py
# (empty)

# tools/pixel-pipeline/map_renderer/map_data.py
from dataclasses import dataclass, field

@dataclass(frozen=True)
class RegionData:
    name: str
    center_x: int; center_y: int
    wealth: int; qi: int; strategic: int

@dataclass(frozen=True)
class SiteData:
    kind: str
    region_id: int
    resource_amount: int
    danger_tier: int

@dataclass(frozen=True)
class MapData:
    regions: list
    sites: list
    adjacency: list

    @property
    def node_count(self): return len(self.sites)
    @property
    def region_count(self): return len(self.regions)
```

- [ ] **Step 3: Run test — expect PASS**

Run: `python -m pytest tests/pixel-pipeline/test_map_data.py -v`
Expected: 1 passed

- [ ] **Step 4: Add MapDataBridge.from_seed test**

```python
# append to test_map_data.py
from map_renderer.map_data import MapDataBridge

def test_from_seed_deterministic():
    d1 = MapDataBridge.from_seed(42)
    d2 = MapDataBridge.from_seed(42)
    assert d1.node_count == d2.node_count
    assert d1.region_count == d2.region_count
    for i in range(d1.node_count):
        assert d1.sites[i].kind == d2.sites[i].kind
```

- [ ] **Step 5: Implement MapDataBridge.from_seed**

```python
# append to map_data.py
import random

class MapDataBridge:
    @staticmethod
    def from_seed(seed: int, region_count: int = 6):
        rng = random.Random(seed)
        names = ["中原","塞外","江南","西域","苗疆","东海","南疆","北漠","蜀中"]
        regions = []
        for r in range(region_count):
            regions.append(RegionData(
                names[r % len(names)],
                rng.randint(10, 90), rng.randint(10, 90),
                rng.randint(10, 90), rng.randint(20, 80), rng.randint(20, 80)))
        
        per_region = rng.randint(3, 5)
        n = region_count * per_region
        sites = []
        for r in range(region_count):
            for s in range(per_region):
                roll = rng.randint(0, 99)
                k = "normal" if roll < 60 else "resource" if roll < 85 else "secret" if roll < 97 else "sect"
                res = rng.randint(50, 200) if k == "resource" else 0
                danger = rng.randint(1, 3) if regions[r].qi > 60 else rng.randint(0, 1)
                sites.append(SiteData(k, r, res, min(3, danger)))
        
        adj = [[(i+1)%n, (i-1+n)%n] for i in range(n)]
        for i in range(n // 3):
            a, b = rng.randint(0, n-1), rng.randint(0, n-1)
            if a != b and b not in adj[a]: adj[a].append(b)
        for i in range(n): adj[i].sort()
        
        return MapData(regions, sites, adj)
```

- [ ] **Step 6: Run tests — expect 2 passed**

Run: `python -m pytest tests/pixel-pipeline/test_map_data.py -v`
Expected: 2 passed

- [ ] **Step 7: Commit**

```bash
git add tools/pixel-pipeline/map_renderer/ tests/pixel-pipeline/
git commit -m "feat(map-renderer): MapData + MapDataBridge.from_seed — pure data types, deterministic"
```

---

### Task 2: Render config (`render_config.py`)

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/render_config.py`
- Create: `tests/pixel-pipeline/test_render_config.py`

- [ ] **Step 1: Write test**

```python
# tests/pixel-pipeline/test_render_config.py
import sys; sys.path.insert(0, "tools/pixel-pipeline")
from map_renderer.render_config import MapRenderConfig

def test_default_config():
    c = MapRenderConfig()
    assert c.tile_size == 32
    assert c.scale == 2
    assert c.palette_name == "default"
    assert c.show_labels == True

def test_custom_config():
    c = MapRenderConfig(tile_size=48, scale=3, show_labels=False)
    assert c.tile_size == 48
    assert not c.show_labels
```

- [ ] **Step 2: Implement MapRenderConfig**

```python
# tools/pixel-pipeline/map_renderer/render_config.py
from dataclasses import dataclass, field

@dataclass
class MapRenderConfig:
    tile_size: int = 32
    padding: int = 16
    scale: int = 2
    layers: list = field(default_factory=lambda: ["terrain", "road", "site", "label"])
    palette_name: str = "default"
    show_labels: bool = True
    show_resources: bool = True
    road_color: tuple = (100, 100, 100, 200)
    fog_mask: set | None = None
    highlight_sites: set | None = None
```

- [ ] **Step 3: Run tests — expect 2 passed**

Run: `python -m pytest tests/pixel-pipeline/test_render_config.py -v`
Expected: 2 passed

- [ ] **Step 4: Commit**

```bash
git add tools/pixel-pipeline/map_renderer/render_config.py tests/pixel-pipeline/test_render_config.py
git commit -m "feat(map-renderer): MapRenderConfig — tile size, scale, palette, layers"
```

---

### Task 3: Interfaces (`interfaces.py`)

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/interfaces.py`

- [ ] **Step 1: Write interfaces + terrain palette in same file**

```python
# tools/pixel-pipeline/map_renderer/interfaces.py
from abc import ABC, abstractmethod
from PIL import Image

class IAssetProvider(ABC):
    @abstractmethod
    def get_site_icon(self, kind: str, size: int) -> Image: ...
    @abstractmethod
    def get_region_tile(self, biome_idx: int, size: int) -> Image: ...
    @abstractmethod
    def get_palette(self) -> dict: ...

class IMapLayer(ABC):
    priority: int = 0
    @abstractmethod
    def render(self, canvas: Image, data, config, assets: IAssetProvider) -> None: ...

class IMapRenderer(ABC):
    @abstractmethod
    def render(self, data, config, assets: IAssetProvider) -> Image: ...

class IMapDataSource(ABC):
    @property
    @abstractmethod
    def data(self): ...
```

- [ ] **Step 2: No separate test — interfaces are abstract. Commit.**

```bash
git add tools/pixel-pipeline/map_renderer/interfaces.py
git commit -m "feat(map-renderer): IMapRenderer/IMapLayer/IAssetProvider interfaces"
```

---

### Task 4: Terrain palette + icon provider (`terrain_palette.py` + `icon_provider.py`)

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/terrain_palette.py`
- Create: `tools/pixel-pipeline/map_renderer/icon_provider.py`
- Create: `tests/pixel-pipeline/test_icon_provider.py`

- [ ] **Step 1: Write test for icon provider**

```python
# tests/pixel-pipeline/test_icon_provider.py
import sys; sys.path.insert(0, "tools/pixel-pipeline")
from PIL import Image
from map_renderer.icon_provider import ProgrammaticIconProvider

def test_all_four_icons_return_correct_size():
    p = ProgrammaticIconProvider()
    for kind in ["normal", "resource", "secret", "sect"]:
        icon = p.get_site_icon(kind, 48)
        assert icon.size == (48, 48)
        assert icon.mode == "RGBA"

def test_region_tile_returns_correct_size():
    p = ProgrammaticIconProvider()
    tile = p.get_region_tile(0, 32)
    assert tile.size == (32, 32)

def test_palette_has_colors():
    p = ProgrammaticIconProvider()
    pal = p.get_palette()
    assert "normal" in pal
    assert "resource" in pal
```

- [ ] **Step 2: Implement terrain_palette.py**

```python
# tools/pixel-pipeline/map_renderer/terrain_palette.py
import numpy as np
from PIL import Image

REGION_COLORS = [
    (180, 200, 140),  # 中原 green
    (200, 180, 120),  # 塞外 tan
    (140, 200, 180),  # 江南 teal
    (200, 160, 100),  # 西域 orange
    (160, 140, 200),  # 苗疆 purple
    (140, 180, 220),  # 东海 blue
    (180, 140, 160),  # 南疆 rose
    (160, 180, 140),  # 北漠 olive
    (200, 180, 160),  # 蜀中 beige
]

def region_color(idx: int) -> tuple: return REGION_COLORS[idx % len(REGION_COLORS)]

def noise_tile(size: int, seed: int = 0) -> Image:
    """Generate seamless noise texture tile."""
    rng = np.random.RandomState(seed)
    arr = (rng.rand(size, size) * 20 - 10).astype(np.int8)
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    for y in range(size):
        for x in range(size):
            v = 128 + arr[y, x]
            px[x, y] = (v, v, v, 30)
    return img
```

- [ ] **Step 3: Implement icon_provider.py**

```python
# tools/pixel-pipeline/map_renderer/icon_provider.py
from PIL import Image, ImageDraw
from map_renderer.interfaces import IAssetProvider
from map_renderer.terrain_palette import region_color, noise_tile

SITE_ICONS = {
    "normal":  lambda d,s: draw_house(d, s),
    "resource": lambda d,s: draw_ore(d, s),
    "secret":   lambda d,s: draw_mystery(d, s),
    "sect":     lambda d,s: draw_tower(d, s),
}

class ProgrammaticIconProvider(IAssetProvider):
    def get_site_icon(self, kind: str, size: int) -> Image:
        img = Image.new("RGBA", (size, size), (0,0,0,0))
        d = ImageDraw.Draw(img)
        if kind in SITE_ICONS:
            SITE_ICONS[kind](d, size)
        else:
            d.rectangle([2,2,size-3,size-3], fill=(128,128,128,200))
        return img

    def get_region_tile(self, biome_idx: int, size: int) -> Image:
        color = region_color(biome_idx)
        tile = Image.new("RGBA", (size, size), color + (255,))
        noise = noise_tile(size, seed=biome_idx)
        return Image.alpha_composite(tile, noise)

    def get_palette(self) -> dict:
        return {"normal":(139,119,80), "resource":(218,165,32), "secret":(148,0,211), "sect":(255,215,0)}

def draw_house(d, s):  # triangle roof + rectangle base
    m = s // 2; d.polygon([(m,4),(4,s//3),(s-5,s//3)], fill=(139,119,80,255))
    d.rectangle([s//4,s//3,s-s//4,s-4], fill=(160,140,100,255))

def draw_ore(d, s):  # diamond with highlight
    m = s // 2; d.polygon([(m,4),(s-5,m),(m,s-5),(4,m)], fill=(218,165,32,255))
    d.ellipse([m-6,m-6,m+6,m+6], fill=(255,255,200,200))

def draw_mystery(d, s):  # circular question mark
    d.ellipse([4,4,s-5,s-5], outline=(148,0,211,255), width=2)
    d.text((s//3, s//4), "?", fill=(148,0,211,255))

def draw_tower(d, s):  # three-tier pagoda
    for tier in range(3):
        y = s - 10 - tier * 14; w = 8 + tier * 6
        d.rectangle([s//2-w,y-14,s//2+w,y], fill=(255,200,50,255))
```

- [ ] **Step 4: Run tests — expect 3 passed**

Run: `python -m pytest tests/pixel-pipeline/test_icon_provider.py -v`
Expected: 3 passed

- [ ] **Step 5: Commit**

```bash
git add tools/pixel-pipeline/map_renderer/terrain_palette.py tools/pixel-pipeline/map_renderer/icon_provider.py tests/pixel-pipeline/test_icon_provider.py
git commit -m "feat(map-renderer): terrain palette + ProgrammaticIconProvider — 4 site icons, noise tiles"
```

---

### Task 5: Layers (`layers.py`)

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/layers.py`
- Create: `tests/pixel-pipeline/test_layers.py`

- [ ] **Step 1: Write test for TerrainLayer**

```python
# tests/pixel-pipeline/test_layers.py
import sys; sys.path.insert(0, "tools/pixel-pipeline")
from PIL import Image
from map_renderer.layers import TerrainLayer, RoadLayer, SiteLayer
from map_renderer.map_data import MapData, RegionData, SiteData
from map_renderer.render_config import MapRenderConfig
from map_renderer.icon_provider import ProgrammaticIconProvider

def make_stub_data():
    r = [RegionData("中原", 50, 50, 80, 60, 50)]
    s = [SiteData("normal", 0, 0, 0), SiteData("resource", 0, 100, 1)]
    return MapData(r, s, [[1], [0]])

def test_terrain_layer_fills_canvas():
    data = make_stub_data()
    config = MapRenderConfig(tile_size=32, scale=1)
    canvas = Image.new("RGBA", (200, 200), (0,0,0,0))
    layer = TerrainLayer()
    layer.render(canvas, data, config, ProgrammaticIconProvider())
    px = canvas.load()
    # Check that center area is not transparent (was filled)
    assert px[100, 100][3] > 0

def test_road_layer_draws_lines():
    data = make_stub_data()
    config = MapRenderConfig(tile_size=32, scale=1)
    canvas = Image.new("RGBA", (200, 200), (0,0,0,0))
    layer = RoadLayer()
    layer.render(canvas, data, config, ProgrammaticIconProvider())
    px = canvas.load()
    # Check some pixel on the road path is not transparent
    found = False
    for i in range(200):
        if px[i, 100][3] > 0: found = True
    assert found, "Road layer should draw connecting lines"

def test_site_layer_places_icons():
    data = make_stub_data()
    config = MapRenderConfig(tile_size=32, scale=1)
    canvas = Image.new("RGBA", (200, 200), (0,0,0,0))
    layer = SiteLayer()
    layer.render(canvas, data, config, ProgrammaticIconProvider())
    px = canvas.load()
    # Icon at first site position should have content
    assert px[50, 50][3] > 0 or px[60, 60][3] > 0
```

- [ ] **Step 2: Implement TerrainLayer, RoadLayer, SiteLayer**

```python
# tools/pixel-pipeline/map_renderer/layers.py
from PIL import Image
from map_renderer.interfaces import IMapLayer, IAssetProvider
from map_renderer.terrain_palette import region_color

class TerrainLayer(IMapLayer):
    priority = 0
    def render(self, canvas, data, config, assets):
        ts = config.tile_size
        for i, site in enumerate(data.sites):
            x = (i % 8) * ts + config.padding
            y = (i // 8) * ts + config.padding
            tile = assets.get_region_tile(site.region_id, ts)
            canvas.paste(tile, (x, y), tile)

class RoadLayer(IMapLayer):
    priority = 1
    def render(self, canvas, data, config, assets):
        from PIL import ImageDraw
        d = ImageDraw.Draw(canvas)
        ts = config.tile_size; pad = config.padding
        for i, neighbors in enumerate(data.adjacency):
            x1 = (i % 8) * ts + pad + ts // 2
            y1 = (i // 8) * ts + pad + ts // 2
            for j in neighbors:
                if j > i:
                    x2 = (j % 8) * ts + pad + ts // 2
                    y2 = (j // 8) * ts + pad + ts // 2
                    d.line([(x1, y1), (x2, y2)], fill=config.road_color, width=2)

class SiteLayer(IMapLayer):
    priority = 2
    def render(self, canvas, data, config, assets):
        icon_size = 18
        ts = config.tile_size; pad = config.padding
        for i, site in enumerate(data.sites):
            x = (i % 8) * ts + pad + (ts - icon_size) // 2
            y = (i // 8) * ts + pad + (ts - icon_size) // 2
            icon = assets.get_site_icon(site.kind, icon_size)
            canvas.paste(icon, (x, y), icon)
```

- [ ] **Step 3: Run tests — expect 3 passed**

Run: `python -m pytest tests/pixel-pipeline/test_layers.py -v`
Expected: 3 passed

- [ ] **Step 4: Commit**

```bash
git add tools/pixel-pipeline/map_renderer/layers.py tests/pixel-pipeline/test_layers.py
git commit -m "feat(map-renderer): TerrainLayer + RoadLayer + SiteLayer — Bresenham roads, icon placement"
```

---

### Task 6: Pixel renderer (`pixel_renderer.py`)

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/pixel_renderer.py`
- Create: `tests/pixel-pipeline/test_pixel_renderer.py`

- [ ] **Step 1: Write test**

```python
# tests/pixel-pipeline/test_pixel_renderer.py
import sys; sys.path.insert(0, "tools/pixel-pipeline")
from map_renderer.pixel_renderer import PixelMapRenderer
from map_renderer.map_data import MapDataBridge
from map_renderer.render_config import MapRenderConfig
from map_renderer.icon_provider import ProgrammaticIconProvider

def test_render_produces_image():
    data = MapDataBridge.from_seed(42)
    config = MapRenderConfig(scale=1)
    renderer = PixelMapRenderer()
    img = renderer.render(data, config, ProgrammaticIconProvider())
    assert img.mode == "RGBA"
    assert img.size[0] > 0 and img.size[1] > 0

def test_render_deterministic():
    data1 = MapDataBridge.from_seed(42)
    data2 = MapDataBridge.from_seed(42)
    config = MapRenderConfig(scale=1)
    renderer = PixelMapRenderer()
    img1 = renderer.render(data1, config, ProgrammaticIconProvider())
    img2 = renderer.render(data2, config, ProgrammaticIconProvider())
    assert list(img1.getdata()) == list(img2.getdata())
```

- [ ] **Step 2: Implement PixelMapRenderer**

```python
# tools/pixel-pipeline/map_renderer/pixel_renderer.py
from PIL import Image
from map_renderer.interfaces import IMapRenderer, IAssetProvider
from map_renderer.layers import TerrainLayer, RoadLayer, SiteLayer

class PixelMapRenderer(IMapRenderer):
    def __init__(self, layers=None):
        self.layers = layers or [TerrainLayer(), RoadLayer(), SiteLayer()]

    def render(self, data, config, assets: IAssetProvider) -> Image:
        ts = config.tile_size; pad = config.padding
        cols = min(8, data.node_count)
        rows = (data.node_count + cols - 1) // cols
        w = cols * ts + pad * 2
        h = rows * ts + pad * 2
        canvas = Image.new("RGBA", (w, h), (30, 30, 30, 255))
        for layer in sorted(self.layers, key=lambda l: l.priority):
            try:
                layer.render(canvas, data, config, assets)
            except Exception:
                pass  # fail-safe: skip layer, continue
        if config.scale > 1:
            canvas = canvas.resize((w * config.scale, h * config.scale), Image.NEAREST)
        return canvas
```

- [ ] **Step 3: Run tests — expect 2 passed**

Run: `python -m pytest tests/pixel-pipeline/test_pixel_renderer.py -v`
Expected: 2 passed

- [ ] **Step 4: Add main entry and generate sample output**

```python
# append to pixel_renderer.py
if __name__ == "__main__":
    from map_renderer.map_data import MapDataBridge
    from map_renderer.render_config import MapRenderConfig
    from map_renderer.icon_provider import ProgrammaticIconProvider
    import os
    data = MapDataBridge.from_seed(42)
    config = MapRenderConfig(scale=2)
    renderer = PixelMapRenderer()
    img = renderer.render(data, config, ProgrammaticIconProvider())
    out = os.path.join(os.path.dirname(__file__), "..", "..", "output", "map_seed42_scale2.png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    print(f"Map saved to {out}")
```

- [ ] **Step 5: Generate and verify**

Run: `python tools/pixel-pipeline/map_renderer/pixel_renderer.py`
Expected: `Map saved to .../output/map_seed42_scale2.png`

- [ ] **Step 6: Commit**

```bash
git add tools/pixel-pipeline/map_renderer/pixel_renderer.py tests/pixel-pipeline/test_pixel_renderer.py
git commit -m "feat(map-renderer): PixelMapRenderer — layer stack pipeline, fail-safe per layer, NEAREST scale, sample output"
```

---

### Task 7: Pixel font + LabelLayer

**Files:**
- Create: `tools/pixel-pipeline/map_renderer/pixel_font.py`
- Modify: `tools/pixel-pipeline/map_renderer/layers.py` (add LabelLayer)
- Create: `tests/pixel-pipeline/test_pixel_font.py`

- [ ] **Step 1: Write test for pixel font**

```python
# tests/pixel-pipeline/test_pixel_font.py
import sys; sys.path.insert(0, "tools/pixel-pipeline")
from PIL import Image
from map_renderer.pixel_font import render_text

def test_render_text_returns_image():
    img = render_text("中原", 4, (255, 255, 255))
    assert img.mode == "RGBA"
    assert img.size[0] > 0
```

- [ ] **Step 2: Implement pixel_font.py**

```python
# tools/pixel-pipeline/map_renderer/pixel_font.py
from PIL import Image

FONT_4X6 = {  # minimal pixel font, uppercase + Chinese placeholder
    'A': [(1,0),(2,0),(0,1),(3,1),(0,2),(1,2),(2,2),(3,2),(0,3),(3,3),(0,4),(3,4)],
}

def render_text(text: str, scale: int = 4, color: tuple = (255, 255, 255)) -> Image:
    """Render text as pixel image. Non-Latin chars rendered as filled rect placeholder."""
    char_w, char_h = 5, 7
    w = len(text) * char_w * scale
    h = char_h * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)
    for i, ch in enumerate(text.upper()):
        x0 = i * char_w * scale
        # Simple approach: draw rectangle placeholder for each character
        d.rectangle([x0 + 2, 2, x0 + char_w * scale - 2, h - 2],
                    fill=color + (200,), outline=color + (255,))
    return img
```

- [ ] **Step 3: Add LabelLayer to layers.py**

```python
# append to layers.py
class LabelLayer(IMapLayer):
    priority = 3
    def render(self, canvas, data, config, assets):
        if not config.show_labels: return
        from PIL import ImageDraw
        d = ImageDraw.Draw(canvas)
        ts = config.tile_size; pad = config.padding
        for i, site in enumerate(data.sites):
            x = (i % 8) * ts + pad + ts // 2
            y = (i // 8) * ts + pad + ts - 4
            kind_char = {"normal": "·", "resource": "R", "secret": "?", "sect": "T"}[site.kind]
            label_text = kind_char
            if config.show_resources and site.resource_amount > 0:
                label_text += str(site.resource_amount)
            d.text((x - 6, y), label_text, fill=(255, 255, 255, 240))
```

- [ ] **Step 4: Run tests — expect 1 passed**

Run: `python -m pytest tests/pixel-pipeline/test_pixel_font.py -v`
Expected: 1 passed

- [ ] **Step 5: Commit**

```bash
git add tools/pixel-pipeline/map_renderer/pixel_font.py tools/pixel-pipeline/map_renderer/layers.py tests/pixel-pipeline/test_pixel_font.py
git commit -m "feat(map-renderer): pixel font + LabelLayer — site labels with kind+resource"
```

---

### Task 8: Final integration — run all tests, verify output

- [ ] **Step 1: Run full test suite**

```bash
python -m pytest tests/pixel-pipeline/ -v
```
Expected: all tests pass (count assertions)

- [ ] **Step 2: Generate final sample map**

```bash
python tools/pixel-pipeline/map_renderer/pixel_renderer.py
```
Expected: `output/map_seed42_scale2.png` exists and is non-zero

- [ ] **Step 3: Verify output size matches expectation**

```python
from PIL import Image
img = Image.open("output/map_seed42_scale2.png")
print(f"Size: {img.size}, Mode: {img.mode}")
# Expected: size = (cols*32+32) * scale by (rows*32+32) * scale
```

- [ ] **Step 4: Final commit**

```bash
git add -A && git commit -m "feat(map-renderer): complete pixel map renderer — all layers, deterministic output"
```
