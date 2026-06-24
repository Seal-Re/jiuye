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
