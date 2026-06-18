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

        // ------- 4-6品: 宝器/道器/灵宝 (batch2) -------
        static IEnumerable<ArtifactDef> TreasureDaoWeaponNuminous()
        {
            // ===== 宝器 Treasure (4品, itemTier=3, basePower=80-150) =====
            // 下品
            yield return A("art_trs_dragon_tooth_sword_i", "龙牙剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { Modules.FlatPen(20) });
            yield return A("art_trs_bronze_tower_i", "镇岳铜塔", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { Modules.FlatDR(15) });
            yield return A("art_trs_silken_rope_i", "天蚕缚仙索", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { Modules.Control("bind", 1) });
            yield return A("art_trs_gold_ring_i", "套宝金环", ArtifactForm.Ring,
                ArtifactFunction.Snatch, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { Modules.Drain("itemTier", 2) });
            yield return A("art_trs_jade_gourd_i", "玉净葫芦", ArtifactForm.Gourd,
                ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 10, "回血气+10") });
            yield return A("art_trs_cloud_fan_i", "踏云扇", ArtifactForm.Fan,
                ArtifactFunction.Escape, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { Modules.Evade(25) });
            yield return A("art_trs_spirit_cauldron_i", "聚灵鼎·下品", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Inferior, 3, 80,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 20, "mana上限+20") });

            // 中品
            yield return A("art_trs_meteor_hammer_c", "流星锤", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Common, 3, 100,
                new[] { Modules.FlatPen(25) });
            yield return A("art_trs_black_tortoise_shield_c", "玄武盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Treasure, QualityTier.Common, 3, 100,
                new[] { Modules.FlatDR(18), Modules.Reflect(1, 4) });
            yield return A("art_trs_entrapment_scroll_c", "困仙图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.Treasure, QualityTier.Common, 3, 100,
                new[] { Modules.Control("trap", 2) });
            yield return A("art_trs_demon_break_sword_c", "破魔剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Common, 3, 100,
                new[] { Modules.FlatPen(20), Modules.CounterMul("evil", 3, 1) });
            yield return A("art_trs_shadow_mirror_c", "遁影镜", ArtifactForm.Mirror,
                ArtifactFunction.Escape, ArtifactGrade.Treasure, QualityTier.Common, 3, 100,
                new[] { Modules.Evade(30) });
            yield return A("art_trs_plum_lamp_c", "寒梅灯", ArtifactForm.Lamp,
                ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Common, 3, 100,
                new[] { new EffectOp(EffectOpKind.AddResource, "soulBond", 3, "soulBond+3") });

            // 上品
            yield return A("art_trs_phoenix_wing_spear_s", "凤翅镏金枪", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Superior, 3, 120,
                new[] { Modules.PenFromResource("itemTier", 6), Modules.FlatPen(18) });
            yield return A("art_trs_myriad_sword_scroll_s", "万剑图", ArtifactForm.Scroll,
                ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Superior, 3, 120,
                new[] { Modules.AoePerTarget(30), Modules.FlatPen(15) });
            yield return A("art_trs_earth_escape_banner_s", "土遁幡", ArtifactForm.Banner,
                ArtifactFunction.Escape, ArtifactGrade.Treasure, QualityTier.Superior, 3, 120,
                new[] { Modules.Evade(35), Modules.FlatDR(10) });
            yield return A("art_trs_heal_lotus_s", "回春莲台", ArtifactForm.Lotus,
                ArtifactFunction.Heal, ArtifactGrade.Treasure, QualityTier.Superior, 3, 120,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 20, "回血气+20") });

            // 极品 — 含多功能复合
            yield return A("art_trs_sun_moon_orbs_m", "日月双珠", ArtifactForm.Orb,
                ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Supreme, 3, 150,
                new[] { Modules.FlatPen(30), Modules.CounterMul("evil", 3, 1), Modules.AoePerTarget(20) },
                secFunc: ArtifactFunction.Trap);
            yield return A("art_trs_soul_bond_cauldron_m", "魂契鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Supreme, 3, 150,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "soulBond", 5, "上限+5"),
                        new EffectOp(EffectOpKind.AddTermWeightStep, "soulBondStep", 1, "权重+1阶") },
                secFunc: ArtifactFunction.Support);

            // ===== 道器 DaoWeapon (5品, itemTier=4, basePower=128-240) =====
            // 下品
            yield return A("art_dao_star_picker_sword_i", "摘星剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Inferior, 4, 128,
                new[] { Modules.PenFromResource("swordWill", 3), Modules.FlatPen(25) });
            yield return A("art_dao_void_mirror_i", "虚空镜", ArtifactForm.Mirror,
                ArtifactFunction.Defense, ArtifactGrade.DaoWeapon, QualityTier.Inferior, 4, 128,
                new[] { Modules.FlatDR(20), Modules.Reflect(1, 3) });
            yield return A("art_dao_soul_binding_rope_i", "困魂索", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.DaoWeapon, QualityTier.Inferior, 4, 128,
                new[] { Modules.Control("bind", 2) });
            yield return A("art_dao_purple_gold_gourd_i", "紫金葫芦·道胚", ArtifactForm.Gourd,
                ArtifactFunction.Snatch, ArtifactGrade.DaoWeapon, QualityTier.Inferior, 4, 128,
                new[] { Modules.Drain("itemTier", 3) });
            yield return A("art_dao_life_cauldron_i", "续命鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Heal, ArtifactGrade.DaoWeapon, QualityTier.Inferior, 4, 128,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 25, "回血气+25") });

            // 中品
            yield return A("art_dao_dragon_slayer_blade_c", "斩龙刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Common, 4, 160,
                new[] { Modules.PenFromResource("qixie", 2), Modules.FlatPen(30) });
            yield return A("art_dao_sky_net_scroll_c", "天罗地网图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.DaoWeapon, QualityTier.Common, 4, 160,
                new[] { Modules.Control("net", 2), Modules.Dot("net_burn", 5, 2) });
            yield return A("art_dao_lightning_banner_c", "引雷幡", ArtifactForm.Banner,
                ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Common, 4, 160,
                new[] { Modules.PenFromResource("thunderCharge", 3), Modules.CounterMul("evil", 3, 1) });
            yield return A("art_dao_vajra_bell_c", "金刚法钟", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.DaoWeapon, QualityTier.Common, 4, 160,
                new[] { Modules.FlatDR(25), Modules.Reflect(1, 3) });
            yield return A("art_dao_beast_taming_flute_c", "驭兽笛", ArtifactForm.Instrument,
                ArtifactFunction.Support, ArtifactGrade.DaoWeapon, QualityTier.Common, 4, 160,
                new[] { new EffectOp(EffectOpKind.AddResource, "bond", 5, "bond+5") });

            // 上品 — 多功能
            yield return A("art_dao_blood_devour_needle_s", "噬血神针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Superior, 4, 192,
                new[] { Modules.PenFromResource("qixue", 3), Modules.Drain("qixie", 4) });
            yield return A("art_dao_soul_return_lotus_s", "还魂莲台", ArtifactForm.Lotus,
                ArtifactFunction.Heal, ArtifactGrade.DaoWeapon, QualityTier.Superior, 4, 192,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 30, "回血+30") });
            yield return A("art_dao_wind_thunder_ring_s", "风雷圈", ArtifactForm.Ring,
                ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Superior, 4, 192,
                new[] { Modules.FlatPen(35), Modules.Evade(20) },
                secFunc: ArtifactFunction.Escape);
            yield return A("art_dao_demon_suppress_hammer_s", "镇魔杵", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Superior, 4, 192,
                new[] { Modules.FlatPen(30), Modules.CounterMul("evil", 3, 1), Modules.FlatDR(15) });
            yield return A("art_dao_five_element_banner_s", "五行旗·道胚", ArtifactForm.Banner,
                ArtifactFunction.Support, ArtifactGrade.DaoWeapon, QualityTier.Superior, 4, 192,
                new[] { Modules.CounterMul("fire", 3, 2), Modules.CounterMul("ice", 3, 2), Modules.FlatDR(12) });

            // 极品
            yield return A("art_dao_primordial_chaos_orb_m", "天地玄黄珠", ArtifactForm.Orb,
                ArtifactFunction.Support, ArtifactGrade.DaoWeapon, QualityTier.Supreme, 4, 240,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 50, "mana上限+50"),
                        Modules.FlatDR(20), Modules.Evade(20) },
                secFunc: ArtifactFunction.Defense);

            // ===== 灵宝 NuminousTreasure (6品, itemTier=5, basePower=192-360) =====
            // 下品
            yield return A("art_num_eight_trigram_mirror_i", "八卦护魂镜", ArtifactForm.Mirror,
                ArtifactFunction.Defense, ArtifactGrade.NuminousTreasure, QualityTier.Inferior, 5, 192,
                new[] { Modules.FlatDR(25), Modules.Reflect(1, 2) });
            yield return A("art_num_beast_trap_scroll_i", "万兽困阵图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.NuminousTreasure, QualityTier.Inferior, 5, 192,
                new[] { Modules.Control("trap", 3) });
            yield return A("art_num_soul_snatch_bell_i", "摄魂钟", ArtifactForm.Bell,
                ArtifactFunction.Snatch, ArtifactGrade.NuminousTreasure, QualityTier.Inferior, 5, 192,
                new[] { Modules.Drain("MoGong", 5), Modules.Control("stun", 1) });

            // 中品
            yield return A("art_num_sword_formation_disk_c", "万剑阵盘", ArtifactForm.ArrayDisk,
                ArtifactFunction.Attack, ArtifactGrade.NuminousTreasure, QualityTier.Common, 5, 240,
                new[] { Modules.AoePerTarget(40), Modules.FlatPen(30) });
            yield return A("art_num_life_bound_cauldron_c", "本命魂鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.NuminousTreasure, QualityTier.Common, 5, 240,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "soulBond", 10, "上限+10"),
                        new EffectOp(EffectOpKind.AddTermWeightStep, "soulBondStep", 2, "权重+2阶") });
            yield return A("art_num_phantom_fan_c", "幻影扇", ArtifactForm.Fan,
                ArtifactFunction.Escape, ArtifactGrade.NuminousTreasure, QualityTier.Common, 5, 240,
                new[] { Modules.Evade(40), Modules.FlatDR(15) });
            yield return A("art_num_blood_oath_axe_c", "血誓斧", ArtifactForm.Axe,
                ArtifactFunction.Attack, ArtifactGrade.NuminousTreasure, QualityTier.Common, 5, 240,
                new[] { Modules.PenFromResource("qixue", 3), Modules.Backlash("bloodCast", 20) });

            // 上品 — 稀有稀有度
            yield return A("art_num_star_shatter_hammer_s", "碎星锤", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.NuminousTreasure, QualityTier.Superior, 5, 288,
                new[] { Modules.FlatPen(50), Modules.CounterMul("body", 3, 2) },
                rarity: EffectRarity.Rare);
            yield return A("art_num_void_escape_talisman_s", "破空符", ArtifactForm.Talisman,
                ArtifactFunction.Escape, ArtifactGrade.NuminousTreasure, QualityTier.Superior, 5, 288,
                new[] { Modules.Evade(50) }, rarity: EffectRarity.Rare);
            yield return A("art_num_soul_ensemble_bell_s", "荡魂钟", ArtifactForm.Bell,
                ArtifactFunction.Trap, ArtifactGrade.NuminousTreasure, QualityTier.Superior, 5, 288,
                new[] { Modules.Control("stun", 2), Modules.Dot("soul_shock", 8, 3) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Rare);

            // 极品
            yield return A("art_num_eight_essence_whip_m", "八灵尺", ArtifactForm.Whip,
                ArtifactFunction.Trap, ArtifactGrade.NuminousTreasure, QualityTier.Supreme, 5, 360,
                new[] { Modules.Control("trap", 3), Modules.FlatDR(30), Modules.Reflect(1, 4) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare);
            yield return A("art_num_divine_fire_cauldron_m", "神火鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Attack, ArtifactGrade.NuminousTreasure, QualityTier.Supreme, 5, 360,
                new[] { Modules.PenFromResource("itemTier", 10), Modules.AoePerTarget(30), Modules.Dot("divine_fire", 15, 3) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "可炼化万物之神火鼎，攻防一体。");
        }

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
