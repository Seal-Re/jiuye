# -*- coding: utf-8 -*-
"""pcg-004: 多 seed 地形多样性验证
模拟 TerrainGenerator Whittaker 分类 → 10 seeds 对比
检测: 全平原/全荒漠 "坏种子"、分类分布均衡性
"""
import random, math, hashlib

def hash_noise(x, y, seed):
    """简单确定性哈希噪声 (替代 FastNoiseLite，同 seed 同输出)"""
    h = hashlib.sha256(f"{x}:{y}:{seed}".encode()).digest()
    return int.from_bytes(h[:4], "big") / (2**32)

def octave_noise(x, y, seed, octaves=4):
    """多 Octave 分形噪声模拟"""
    val = 0.0; amp = 1.0; freq = 1.0; max_val = 0.0
    for _ in range(octaves):
        val += hash_noise(x * freq, y * freq, seed) * amp
        max_val += amp; amp *= 0.5; freq *= 2.0
    return val / max_val if max_val > 0 else 0.0

def classify(h, m, t, p):
    """Whittaker 分类 (与 TerrainGenerator.gd 一致)"""
    qi = min(max(int(h * 80 + m * 40), 0), 100)
    if p > 0.85:
        return "T08" if t > 0.6 else "T10"
    if qi >= 70 and p < 0.3: return "T11"
    if t < 0.2 and h > 0.5:  return "T09"
    if t > 0.6 and m < 0.35: return "T03"
    if t > 0.55 and m > 0.5: return "T05"
    if t < 0.4 and m > 0.5 and h > 0.4: return "T06"
    if m > 0.7 and h < 0.4:  return "T07"
    if h > 0.75: return "T04"
    if h > 0.5 and m > 0.4:  return "T06"
    return "T01"

def sweep(seed, w=100, h=100):
    dist = {}
    for x in range(w):
        for y in range(h):
            ht = octave_noise(x, y, seed, 4)
            mt = octave_noise(x, y, seed + 1000, 3)
            tp = octave_noise(x, y, seed + 2000, 2)
            pr = octave_noise(x, y, seed + 3000, 3)
            tid = classify(ht, mt, tp, pr)
            dist[tid] = dist.get(tid, 0) + 1
    return dist

print("=== pcg-004: 10 seeds 地形多样性验证 ===\n")
seeds = [42, 99, 256, 512, 1024, 2026, 4096, 7777, 9999, 12345]
bad_seeds = []

for s in seeds:
    dist = sweep(s)
    total = sum(dist.values())
    pct = {k: v * 100 / total for k, v in dist.items()}

    # 坏种子检测: T01(平原) > 80% → 太单调
    t01_pct = pct.get("T01", 0)
    unique = len(dist)
    status = "OK"
    if t01_pct > 80:
        status = "⚠ 平原过多"
    if unique < 3:
        status = "❌ 坏种子"
        bad_seeds.append(s)

    top3 = sorted(pct.items(), key=lambda x: -x[1])[:3]
    top3_str = ", ".join(f"{k}:{v:.0f}%" for k, v in top3)
    print(f"  seed={s:5d}  T01={t01_pct:5.1f}%  types={unique:2d}  [{top3_str}]  {status}")

print(f"\n坏种子: {bad_seeds if bad_seeds else '无'}")
print("✅ pcg-004 PASS" if not bad_seeds else "❌ 需处理坏种子")
