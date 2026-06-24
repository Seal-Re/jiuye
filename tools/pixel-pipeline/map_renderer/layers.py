from PIL import Image, ImageDraw
from map_renderer.interfaces import IMapLayer, IAssetProvider

class TerrainLayer(IMapLayer):
    priority = 0
    def render(self, canvas, data, config, assets):
        ts = config.tile_size
        cols = min(8, data.node_count)
        for i, site in enumerate(data.sites):
            x = (i % cols) * ts + config.padding
            y = (i // cols) * ts + config.padding
            tile = assets.get_region_tile(site.region_id, ts)
            canvas.paste(tile, (x, y), tile)

class RoadLayer(IMapLayer):
    priority = 1
    def render(self, canvas, data, config, assets):
        d = ImageDraw.Draw(canvas)
        ts = config.tile_size; pad = config.padding
        cols = min(8, data.node_count)
        for i, neighbors in enumerate(data.adjacency):
            x1 = (i % cols) * ts + pad + ts // 2
            y1 = (i // cols) * ts + pad + ts // 2
            for j in neighbors:
                if j > i:
                    x2 = (j % cols) * ts + pad + ts // 2
                    y2 = (j // cols) * ts + pad + ts // 2
                    d.line([(x1, y1), (x2, y2)], fill=config.road_color, width=2)

class SiteLayer(IMapLayer):
    priority = 2
    def render(self, canvas, data, config, assets):
        icon_size = 18
        ts = config.tile_size; pad = config.padding
        cols = min(8, data.node_count)
        for i, site in enumerate(data.sites):
            x = (i % cols) * ts + pad + (ts - icon_size) // 2
            y = (i // cols) * ts + pad + (ts - icon_size) // 2
            icon = assets.get_site_icon(site.kind, icon_size)
            canvas.paste(icon, (x, y), icon)

class LabelLayer(IMapLayer):
    priority = 3
    def render(self, canvas, data, config, assets):
        if not config.show_labels: return
        d = ImageDraw.Draw(canvas)
        ts = config.tile_size; pad = config.padding
        cols = min(8, data.node_count)
        for i, site in enumerate(data.sites):
            x = (i % cols) * ts + pad + ts // 2
            y = (i // cols) * ts + pad + ts - 4
            kind_char = {"normal": ".", "resource": "R", "secret": "?", "sect": "T"}[site.kind]
            label = kind_char
            if config.show_resources and site.resource_amount > 0:
                label += str(site.resource_amount // 10)
            d.text((x - 6, y), label, fill=(255, 255, 255, 240))
