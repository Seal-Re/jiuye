from dataclasses import dataclass
import random

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
