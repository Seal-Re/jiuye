import sys; sys.path.insert(0, "tools/pixel-pipeline")
from map_renderer.pixel_renderer import PixelMapRenderer
from map_renderer.map_data import MapDataBridge
from map_renderer.render_config import MapRenderConfig
from map_renderer.icon_provider import ProgrammaticIconProvider

def test_render_produces_image():
    data = MapDataBridge.from_seed(42)
    config = MapRenderConfig(scale=1)
    img = PixelMapRenderer().render(data, config, ProgrammaticIconProvider())
    assert img.mode == "RGBA"
    assert img.size[0] > 0 and img.size[1] > 0

def test_render_deterministic():
    data1 = MapDataBridge.from_seed(42)
    data2 = MapDataBridge.from_seed(42)
    config = MapRenderConfig(scale=1)
    img1 = PixelMapRenderer().render(data1, config, ProgrammaticIconProvider())
    img2 = PixelMapRenderer().render(data2, config, ProgrammaticIconProvider())
    assert list(img1.getdata()) == list(img2.getdata())
