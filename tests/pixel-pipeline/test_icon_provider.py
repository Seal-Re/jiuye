import sys; sys.path.insert(0, "tools/pixel-pipeline")
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
