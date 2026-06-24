from PIL import Image
from map_renderer.interfaces import IMapRenderer, IAssetProvider
from map_renderer.layers import TerrainLayer, RoadLayer, SiteLayer, LabelLayer

class PixelMapRenderer(IMapRenderer):
    def __init__(self, layers=None):
        self._layers = layers or [TerrainLayer(), RoadLayer(), SiteLayer(), LabelLayer()]

    def render(self, data, config, assets: IAssetProvider) -> Image:
        ts = config.tile_size; pad = config.padding
        cols = min(8, max(1, data.node_count))
        rows = (data.node_count + cols - 1) // cols
        w = cols * ts + pad * 2
        h = rows * ts + pad * 2
        canvas = Image.new("RGBA", (w, h), (30, 30, 30, 255))
        for layer in sorted(self._layers, key=lambda l: l.priority):
            try:
                layer.render(canvas, data, config, assets)
            except Exception:
                pass  # fail-safe: skip layer, continue
        if config.scale > 1:
            canvas = canvas.resize((w * config.scale, h * config.scale), Image.NEAREST)
        return canvas

if __name__ == "__main__":
    import os, sys
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    from PIL import Image
    from map_renderer.map_data import MapDataBridge
    from map_renderer.render_config import MapRenderConfig
    from map_renderer.icon_provider import ProgrammaticIconProvider
    from map_renderer.layers import TerrainLayer, RoadLayer, SiteLayer, LabelLayer

    data = MapDataBridge.from_seed(42)
    config = MapRenderConfig(scale=2)
    renderer = PixelMapRenderer()
    img = renderer.render(data, config, ProgrammaticIconProvider())
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "output", "map_seed42_scale2.png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    print(f"Map saved to {out} ({img.size[0]}x{img.size[1]})")
