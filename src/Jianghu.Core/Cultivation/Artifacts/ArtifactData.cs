using System.Collections.Generic;

namespace Jianghu.Cultivation.Artifacts
{
    public static partial class ArtifactData
    {
        public static IReadOnlyList<ArtifactDef> All { get; } = BuildAll();

        static IReadOnlyList<ArtifactDef> BuildAll()
        {
            var list = new List<ArtifactDef>();
            list.AddRange(MortalDharmaSpirit());
            list.AddRange(TreasureDaoWeaponNuminous());
            list.AddRange(HeavenReachingProfoundSkyPrimordial());
            list.AddRange(UniqueArtifacts());
            return list;
        }

        public static ArtifactRegistry DefaultRegistry => new ArtifactRegistry(All);

        static EffectOp[] NoFx => System.Array.Empty<EffectOp>();

        // ------- 1-3品: 凡器/法器/灵器 (攻/防为主) -------
        static IEnumerable<ArtifactDef> MortalDharmaSpirit()
        {
            // 凡器 Mortal (itemTier=0, basePower=8-15) — 下品
            yield return A("art_mor_iron_sword_i", "凡铁剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);
            yield return A("art_mor_copper_blade_i", "黄铜刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);
            yield return A("art_mor_wood_spear_i", "木杆枪", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);
            yield return A("art_mor_stone_seal_i", "石印", ArtifactForm.Seal,
                ArtifactFunction.Trap, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);
            yield return A("art_mor_leather_shield_i", "皮盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);
            yield return A("art_mor_bone_needle_i", "骨针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);
            yield return A("art_mor_bamboo_whip_i", "竹鞭", ArtifactForm.Whip,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior, 0, 8, NoFx);

            // 凡器 — 中品
            yield return A("art_mor_iron_sword_c", "锻铁剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Common, 0, 10, NoFx);
            yield return A("art_mor_bronze_blade_c", "青铜刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Common, 0, 10, NoFx);
            yield return A("art_mor_hemp_rope_c", "麻绳", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Mortal, QualityTier.Common, 0, 10, NoFx);
            yield return A("art_mor_wood_shield_c", "木盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Mortal, QualityTier.Common, 0, 10, NoFx);
            yield return A("art_mor_stone_axe_c", "石斧", ArtifactForm.Axe,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Common, 0, 10, NoFx);

            // 凡器 — 上品
            yield return A("art_mor_steel_sword_s", "精钢剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Superior, 0, 12, NoFx);
            yield return A("art_mor_hardwood_bow_s", "硬木弓", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Superior, 0, 12, NoFx);
            yield return A("art_mor_iron_fan_s", "铁骨扇", ArtifactForm.Fan,
                ArtifactFunction.Trap, ArtifactGrade.Mortal, QualityTier.Superior, 0, 12, NoFx);

            // 凡器 — 极品
            yield return A("art_mor_hundred_refine_sword_m", "百炼精钢剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Supreme, 0, 15, NoFx);
            yield return A("art_mor_iron_tower_m", "铁塔盾", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.Mortal, QualityTier.Supreme, 0, 15, NoFx);

            // 法器 Dharma (itemTier=1, basePower=24-45) — 下品
            yield return A("art_dha_cold_iron_sword_i", "寒铁剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24,
                new[] { Modules.FlatPen(5) });
            yield return A("art_dha_fire_tip_spear_i", "火尖枪·凡胚", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24,
                new[] { Modules.FlatPen(5) });
            yield return A("art_dha_luo_hammer_i", "落石锤", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24,
                new[] { Modules.FlatPen(6) });
            yield return A("art_dha_soul_lock_rope_i", "锁魂绳", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24, NoFx);
            yield return A("art_dha_spirit_wood_shield_i", "灵木盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24,
                new[] { Modules.FlatDR(5) });
            yield return A("art_dha_shadow_needle_i", "无影针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24,
                new[] { Modules.FlatPen(4) });
            yield return A("art_dha_thunder_whip_i", "风雷鞭", ArtifactForm.Whip,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior, 1, 24,
                new[] { Modules.FlatPen(5) });

            // 法器 — 中品
            yield return A("art_dha_black_iron_sword_c", "黑铁重剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Common, 1, 30,
                new[] { Modules.FlatPen(8) });
            yield return A("art_dha_wind_fire_fan_c", "风火扇·初炼", ArtifactForm.Fan,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Common, 1, 30,
                new[] { Modules.FlatPen(10) });
            yield return A("art_dha_iron_bell_c", "镇魂铁钟", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Common, 1, 30,
                new[] { Modules.FlatDR(6) });
            yield return A("art_dha_shadow_mirror_c", "窥影镜", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.Dharma, QualityTier.Common, 1, 30,
                new[] { Modules.CounterMul("ghost", 3, 2) });
            yield return A("art_dha_soul_suppress_seal_c", "镇魂印", ArtifactForm.Seal,
                ArtifactFunction.Trap, ArtifactGrade.Dharma, QualityTier.Common, 1, 30,
                new[] { Modules.Control("suppress", 1) });

            // 法器 — 上品
            yield return A("art_dha_thunder_needle_s", "雷芒针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Superior, 1, 36,
                new[] { Modules.FlatPen(12) });
            yield return A("art_dha_ice_mirror_s", "寒冰镜", ArtifactForm.Mirror,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Superior, 1, 36,
                new[] { Modules.FlatDR(8) });
            yield return A("art_dha_vajra_shield_s", "金刚盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Superior, 1, 36,
                new[] { Modules.FlatDR(10) });
            yield return A("art_dha_mountain_axe_s", "开山斧", ArtifactForm.Axe,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Superior, 1, 36,
                new[] { Modules.FlatPen(10) });

            // 法器 — 极品
            yield return A("art_dha_flame_sword_m", "烈焰剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Supreme, 1, 45,
                new[] { Modules.FlatPen(15), Modules.CounterMul("ice", 3, 2) });

            // 灵器 Spirit (itemTier=2, basePower=48-90) — 下品
            yield return A("art_spr_azure_edge_sword_i", "青锋灵剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Inferior, 2, 48,
                new[] { Modules.FlatPen(12) });
            yield return A("art_spr_soul_devour_blade_i", "噬魂刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Inferior, 2, 48,
                new[] { Modules.PenFromResource("shaCharge", 1, 2) });
            yield return A("art_spr_binding_rope_i", "缚灵索", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Inferior, 2, 48,
                new[] { Modules.Control("bind", 1) });
            yield return A("art_spr_iron_body_bell_i", "金钟罩·初", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.Spirit, QualityTier.Inferior, 2, 48,
                new[] { Modules.FlatDR(10) });
            yield return A("art_spr_brass_mirror_i", "照妖铜镜", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.Spirit, QualityTier.Inferior, 2, 48,
                new[] { Modules.CounterMul("evil", 2, 1) });
            yield return A("art_spr_green_lotus_lamp_i", "青莲灯", ArtifactForm.Lamp,
                ArtifactFunction.Heal, ArtifactGrade.Spirit, QualityTier.Inferior, 2, 48,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 10, "青莲灯回血气+10") });

            // 灵器 — 中品
            yield return A("art_spr_red_line_needle_c", "红线遁光针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Common, 2, 60,
                new[] { Modules.PenFromResource("itemTier", 3) });
            yield return A("art_spr_thunder_seal_c", "雷光印", ArtifactForm.Seal,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Common, 2, 60,
                new[] { Modules.FlatPen(16), Modules.CounterMul("evil", 3, 2) });
            yield return A("art_spr_venom_gourd_c", "百毒葫芦", ArtifactForm.Gourd,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Common, 2, 60,
                new[] { Modules.Dot("venom", 3, 3) });
            yield return A("art_spr_escape_banner_c", "遁光幡", ArtifactForm.Banner,
                ArtifactFunction.Escape, ArtifactGrade.Spirit, QualityTier.Common, 2, 60,
                new[] { Modules.Evade(20) });
            yield return A("art_spr_heal_jade_lamp_c", "回春玉灯", ArtifactForm.Lamp,
                ArtifactFunction.Heal, ArtifactGrade.Spirit, QualityTier.Common, 2, 60,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 15, "回血气+15") });
            yield return A("art_spr_spirit_whip_c", "灵蛇鞭", ArtifactForm.Whip,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Common, 2, 60,
                new[] { Modules.FlatPen(12), Modules.Control("entangle", 1) });

            // 灵器 — 上品
            yield return A("art_spr_flying_sword_wind_s", "御风飞剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Superior, 2, 72,
                new[] { Modules.PenFromResource("itemTier", 5) });
            yield return A("art_spr_sand_fan_s", "飞沙扇", ArtifactForm.Fan,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Superior, 2, 72,
                new[] { Modules.Dot("sand_storm", 4, 2) });
            yield return A("art_spr_vajra_hammer_s", "伏魔金刚杵", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Superior, 2, 72,
                new[] { Modules.FlatPen(20), Modules.CounterMul("evil", 3, 1) });
            yield return A("art_spr_soul_capture_mirror_s", "摄魂镜", ArtifactForm.Mirror,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Superior, 2, 72,
                new[] { Modules.Control("soul_lock", 2) });

            // 灵器 — 极品
            yield return A("art_spr_bamboo_swarm_sword_m", "青竹蜂云剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Supreme, 2, 90,
                new[] { Modules.PenFromResource("itemTier", 8), Modules.AoePerTarget(12) });
            yield return A("art_spr_luo_ring_m", "落宝圈·初胚", ArtifactForm.Ring,
                ArtifactFunction.Snatch, ArtifactGrade.Spirit, QualityTier.Supreme, 2, 90,
                new[] { Modules.Drain("itemTier", 1) });
        }

        // ------- 4-6品 placeholder (batch2) -------
        static IEnumerable<ArtifactDef> TreasureDaoWeaponNuminous() { yield break; }

        // ------- 7-9品 placeholder (batch3) -------
        static IEnumerable<ArtifactDef> HeavenReachingProfoundSkyPrimordial() { yield break; }

        // ------- Unique placeholder (batch4) -------
        static IEnumerable<ArtifactDef> UniqueArtifacts() { yield break; }

        // helper factory
        static ArtifactDef A(string id, string name, ArtifactForm form,
            ArtifactFunction func, ArtifactGrade grade, QualityTier quality,
            int itemTier, int power, IReadOnlyList<EffectOp> fx,
            ArtifactFunction? secFunc = null, EffectRarity rarity = EffectRarity.Common,
            string? elem = null, string? flavor = null, string? src = null)
            => new ArtifactDef(id, name, form, func, secFunc, grade, quality,
                itemTier, power, fx, rarity, elem, flavor ?? name, src ?? "江湖流通");
    }
}
