import sys; sys.path.insert(0, "tools/pixel-pipeline")
from map_renderer.map_data import MapData, RegionData, SiteData, MapDataBridge

def test_map_data_from_regions():
    regions = [RegionData("中原", 50, 50, 80, 60, 50)]
    sites = [SiteData("normal", 0, 0, 0), SiteData("resource", 0, 100, 1)]
    adj = [[1], [0]]
    data = MapData(regions, sites, adj)
    assert data.node_count == 2
    assert data.region_count == 1
    assert data.sites[1].kind == "resource"
    assert data.adjacency[0] == [1]

def test_from_seed_deterministic():
    d1 = MapDataBridge.from_seed(42)
    d2 = MapDataBridge.from_seed(42)
    assert d1.node_count == d2.node_count
    assert d1.region_count == d2.region_count
    for i in range(d1.node_count):
        assert d1.sites[i].kind == d2.sites[i].kind
