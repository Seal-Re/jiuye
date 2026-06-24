import sys; sys.path.insert(0, "tools/pixel-pipeline")
from PIL import Image
from map_renderer.layers import TerrainLayer, RoadLayer, SiteLayer
from map_renderer.map_data import MapData, RegionData, SiteData
from map_renderer.render_config import MapRenderConfig
from map_renderer.icon_provider import ProgrammaticIconProvider

def make_stub():
    r = [RegionData("中原", 50, 50, 80, 60, 50)]
    s = [SiteData("normal", 0, 0, 0), SiteData("resource", 0, 100, 1)]
    return MapData(r, s, [[1], [0]])

def test_terrain_layer_fills_canvas():
    data = make_stub()
    config = MapRenderConfig(tile_size=32, scale=1, padding=16)
    canvas = Image.new("RGBA", (200, 200), (0,0,0,0))
    TerrainLayer().render(canvas, data, config, ProgrammaticIconProvider())
    px = canvas.load()
    # Site 0 center: 16+16=32, 16+16=32
    assert px[32, 32][3] > 0, "terrain should fill tile at site position"

def test_road_layer_draws_lines():
    data = make_stub()
    config = MapRenderConfig(tile_size=32, scale=1, padding=16)
    canvas = Image.new("RGBA", (200, 200), (0,0,0,0))
    RoadLayer().render(canvas, data, config, ProgrammaticIconProvider())
    px = canvas.load()
    # Road from site 0 (32,32) to site 1 (64,32)
    assert any(px[i, 32][3] > 0 for i in range(200)), "road should draw between sites"

def test_site_layer_places_icons():
    data = make_stub()
    config = MapRenderConfig(tile_size=32, scale=1, padding=16)
    canvas = Image.new("RGBA", (200, 200), (0,0,0,0))
    SiteLayer().render(canvas, data, config, ProgrammaticIconProvider())
    px = canvas.load()
    # Search for any non-transparent pixel in the icon area
    found = False
    for dy in range(30):
        for dx in range(30):
            if px[20+dx, 20+dy][3] > 0:
                found = True; break
        if found: break
    assert found, "site icon should place visible pixels"
