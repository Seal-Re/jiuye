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
