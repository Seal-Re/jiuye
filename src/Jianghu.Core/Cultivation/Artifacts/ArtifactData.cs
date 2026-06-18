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

        // ------- 7-9品: 通天灵宝/玄天之宝/混沌至宝 (batch3) -------
        static IEnumerable<ArtifactDef> HeavenReachingProfoundSkyPrimordial()
        {
            // ===== 通天灵宝 HeavenReaching (7品, itemTier=6, basePower=340) =====
            // 下品(272)
            yield return A("art_hr_abyss_frost_sword_i", "寒渊破虚剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Inferior, 6, 272,
                new[] { Modules.PenFromResource("swordWill", 6), Modules.FlatPen(40) },
                rarity: EffectRarity.Rare,
                flavor: "采九幽寒铁铸就，出鞘则虚空凝霜。剑修合体期标志性法宝。");
            yield return A("art_hr_mountain_suppress_tower_i", "镇岳玄黄塔", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Inferior, 6, 272,
                new[] { Modules.FlatDR(45), Modules.Reflect(1, 3) },
                rarity: EffectRarity.Rare,
                flavor: "以玄黄之气凝成七层宝塔，可镇山河、定乾坤。");
            yield return A("art_hr_void_escape_talisman_i", "破虚遁空符", ArtifactForm.Talisman,
                ArtifactFunction.Escape, ArtifactGrade.HeavenReaching, QualityTier.Inferior, 6, 272,
                new[] { Modules.Evade(60) },
                rarity: EffectRarity.Rare,
                flavor: "合体期遁术至宝，一符破虚、瞬息万里。");
            yield return A("art_hr_soul_bind_rope_i", "缚魂锁仙索", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Inferior, 6, 272,
                new[] { Modules.Control("bind", 3), Modules.Drain("manaPool", 5) },
                rarity: EffectRarity.Rare,
                flavor: "以魂丝织成的锁仙索，可封困元神、锁死法力。");
            yield return A("art_hr_soul_return_lamp_i", "还魂续命灯", ArtifactForm.Lamp,
                ArtifactFunction.Heal, ArtifactGrade.HeavenReaching, QualityTier.Inferior, 6, 272,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 40, "回血气+40") },
                rarity: EffectRarity.Rare,
                flavor: "燃千年魂灯，可续一息残命。");

            // 中品(340)
            yield return A("art_hr_violet_thunder_hammer_c", "紫电雷光锤", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Common, 6, 340,
                new[] { Modules.PenFromResource("thunderCharge", 5), Modules.FlatPen(50), Modules.CounterMul("evil", 3, 1) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "引九天神雷淬炼而成的雷锤，一锤之下万雷齐发。");
            yield return A("art_hr_army_breaker_axe_c", "破军裂天斧", ArtifactForm.Axe,
                ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Common, 6, 340,
                new[] { Modules.FlatPen(55), Modules.AoePerTarget(35), Modules.Backlash("burnGate", 30) },
                rarity: EffectRarity.Rare,
                flavor: "上古战神遗留的裂天斧，一斧可破万军。但每一击皆噬主精元。");
            yield return A("art_hr_tortoise_shield_c", "玄武不灭盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Common, 6, 340,
                new[] { Modules.FlatDR(50), Modules.Reflect(1, 2), Modules.Evade(25) },
                rarity: EffectRarity.Rare,
                flavor: "以北冥玄龟甲炼成，盾坚不破、反震万钧。");
            yield return A("art_hr_soul_banner_c", "万魂噬天幡", ArtifactForm.Banner,
                ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Common, 6, 340,
                new[] { Modules.PenFromResource("ghostSoldierPower", 5), Modules.Drain("shaCharge", 8) },
                secFunc: ArtifactFunction.Snatch, rarity: EffectRarity.Rare,
                flavor: "幡中炼化万魂为兵，遮天蔽日、噬魂夺魄。");
            yield return A("art_hr_eight_trigram_mirror_c", "八卦封天镜", ArtifactForm.Mirror,
                ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Common, 6, 340,
                new[] { Modules.FlatDR(40), Modules.Reflect(2, 3), Modules.Control("seal", 2) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "八卦运转、镜光封天，可封印一切外道。");

            // 上品(408)
            yield return A("art_hr_luobao_money_s", "落宝金钱", ArtifactForm.Ring,
                ArtifactFunction.Snatch, ArtifactGrade.HeavenReaching, QualityTier.Superior, 6, 408,
                new[] { Modules.Special("luobao", 6, 0, "落宝金钱"), Modules.Drain("itemTier", 5) },
                secFunc: ArtifactFunction.Support, rarity: EffectRarity.Rare,
                flavor: "通天灵宝上品·落宝金钱，可落尽天下法宝。一宝落尽万宝空。");
            yield return A("art_hr_overturn_heaven_seal_s", "翻天印", ArtifactForm.Seal,
                ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Superior, 6, 408,
                new[] { Modules.FlatPen(65), Modules.CounterMul("body", 3, 1), Modules.AoePerTarget(30) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "翻天印，一印翻天覆地。体修遇之亦难当。");
            yield return A("art_hr_primal_chaos_orb_s", "混元一气珠", ArtifactForm.Orb,
                ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Superior, 6, 408,
                new[] { Modules.PenFromResource("manaPool", 2), Modules.FlatPen(50), Modules.FlatDR(25) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "混元一气化珠，攻防一体、变化万千。");
            yield return A("art_hr_nine_bend_array_s", "九曲黄河阵图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Superior, 6, 408,
                new[] { Modules.Control("trap", 4), Modules.Dot("river_drown", 20, 3), Modules.Drain("manaPool", 8) },
                rarity: EffectRarity.Rare,
                flavor: "九曲黄河阵，陷仙困圣、消魂蚀骨。入阵者十死无生。");

            // 极品(510)
            yield return A("art_hr_12_golden_lotus_m", "十二品金莲", ArtifactForm.Lotus,
                ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Supreme, 6, 510,
                new[] { Modules.FlatDR(60), Modules.Reflect(2, 3), Modules.Evade(35),
                    new EffectOp(EffectOpKind.AddResource, "qixue", 25, "回血气+25") },
                secFunc: ArtifactFunction.Heal, rarity: EffectRarity.Rare,
                flavor: "十二品金莲，万法不侵、功德无量。佛修至高防宝。");
            yield return A("art_hr_24_sea_fixing_orbs_m", "二十四颗定海珠", ArtifactForm.Orb,
                ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Supreme, 6, 510,
                new[] { Modules.Control("flood", 5), Modules.Dot("sea_pressure", 25, 4), Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "二十四颗定海神珠，可演化二十四诸天。颗颗皆含一界之力。");
            yield return A("art_hr_sword_formation_fake_m", "诛仙阵图·通灵仿", ArtifactForm.ArrayDisk,
                ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Supreme, 6, 510,
                new[] { Modules.Special("explodeArray", 6, 0, "诛仙剑意·仿"), Modules.Control("trap", 4) },
                rarity: EffectRarity.Rare,
                flavor: "仿诛仙剑阵而制的阵盘，虽不及真品万一，亦足以惊退大乘。");

            // ===== 玄天之宝 ProfoundSky (8品, itemTier=7, basePower=480) =====
            // 下品(384)
            yield return A("art_ps_void_pierce_sword_i", "破虚裂空剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Inferior, 7, 384,
                new[] { Modules.PenFromResource("swordWill", 8), Modules.FlatPen(55), Modules.CounterMul("void", 3, 2) },
                rarity: EffectRarity.Rare,
                flavor: "大乘期剑修玄天至宝，一剑出则虚空碎裂。");
            yield return A("art_ps_sky_net_umbrella_i", "天罗混元伞", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Inferior, 7, 384,
                new[] { Modules.FlatDR(55), Modules.Reflect(2, 3), Modules.Evade(30) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "撑开可遮天蔽日，收拢可纳山河。玄天级防宝。");
            yield return A("art_ps_dragon_trap_pillar_i", "遁龙桩", ArtifactForm.Seal,
                ArtifactFunction.Trap, ArtifactGrade.ProfoundSky, QualityTier.Inferior, 7, 384,
                new[] { Modules.Control("bind", 4), Modules.Drain("manaPool", 8) },
                rarity: EffectRarity.Rare,
                flavor: "遁龙桩，仙神难逃。困龙锁仙只在一念之间。");
            yield return A("art_ps_demon_summon_banner_i", "招妖幡", ArtifactForm.Banner,
                ArtifactFunction.Support, ArtifactGrade.ProfoundSky, QualityTier.Inferior, 7, 384,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "bond", 20, "bond上限+20"),
                    Modules.PenFromResource("bond", 2) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Rare,
                flavor: "招妖幡动，万妖听令。御兽/傀儡修士梦寐之宝。");

            // 中品(480)
            yield return A("art_ps_chaos_bell_fake_c", "混沌钟·仿", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Common, 7, 480,
                new[] { Modules.FlatDR(65), Modules.Reflect(2, 3), Modules.Control("stun", 3) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "仿混沌至宝混沌钟而制，钟声一响镇压时空。");
            yield return A("art_ps_taiji_scroll_fake_c", "太极图·仿", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.ProfoundSky, QualityTier.Common, 7, 480,
                new[] { Modules.Control("trap", 4), Modules.Dot("yin_yang_grind", 25, 3), Modules.FlatDR(35) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "仿太极图而炼，阴阳二气化桥、困杀阵中一切。");
            yield return A("art_ps_pangu_banner_fake_c", "盘古幡·仿", ArtifactForm.Banner,
                ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Common, 7, 480,
                new[] { Modules.FlatPen(75), Modules.AoePerTarget(45), Modules.CounterMul("void", 3, 1) },
                rarity: EffectRarity.Rare,
                flavor: "仿盘古幡的无上攻伐至宝，幡动则混沌裂、万界崩。");
            yield return A("art_ps_immortal_slayer_fake_c", "斩仙飞刀·仿", ArtifactForm.Gourd,
                ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Common, 7, 480,
                new[] { Modules.FlatPen(70), Modules.Special("duoshe", 4, 0, "斩仙·仿") },
                secFunc: ArtifactFunction.Snatch, rarity: EffectRarity.Rare,
                flavor: "葫芦口出白光一线，定住元神即斩。仿品已具真品三分威能。");

            // 上品(576)
            yield return A("art_ps_heavenly_dao_wheel_s", "天道轮盘", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.ProfoundSky, QualityTier.Superior, 7, 576,
                new[] { Modules.Special("reverseStack", 4, 0, "逆演回滚"),
                    new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 2, "道心权重+2阶"),
                    Modules.FlatDR(30) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "天道轮盘，逆转因果。以道心驱动，可回滚一回合。");
            yield return A("art_ps_spacetime_mirror_s", "时空万象镜", ArtifactForm.Mirror,
                ArtifactFunction.Escape, ArtifactGrade.ProfoundSky, QualityTier.Superior, 7, 576,
                new[] { Modules.Evade(70), Modules.Control("time_lock", 2) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "时空镜中映万象，一瞬千年。大乘期顶级遁宝。");
            yield return A("art_ps_myriad_calamity_bell_s", "万劫不灭钟", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Superior, 7, 576,
                new[] { Modules.FlatDR(70), Modules.Reflect(2, 3), Modules.Evade(40) },
                rarity: EffectRarity.Rare,
                flavor: "历经万劫而不灭的护体玄钟，钟声所在即为不可侵犯之域。");

            // 极品(720)
            yield return A("art_ps_jade_disc_fragment_m", "造化玉碟·残", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.ProfoundSky, QualityTier.Supreme, 7, 720,
                new[] { new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 3, "道心权重+3阶"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 80, "mana上限+80"),
                    Modules.FlatDR(35) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "造化玉碟残片，虽残仍含三千大道之精华。悟道至宝。");
            yield return A("art_ps_sky_open_orb_m", "开天珠", ArtifactForm.Orb,
                ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Supreme, 7, 720,
                new[] { Modules.FlatPen(90), Modules.AoePerTarget(50), Modules.CounterMul("void", 4, 2) },
                rarity: EffectRarity.Rare,
                flavor: "先天灵宝开天珠，蕴含开天辟地之力。一击可碎星辰。");
            yield return A("art_ps_earth_split_orb_m", "辟地珠", ArtifactForm.Orb,
                ArtifactFunction.Trap, ArtifactGrade.ProfoundSky, QualityTier.Supreme, 7, 720,
                new[] { Modules.Control("earth_split", 5), Modules.Dot("earth_collapse", 30, 3) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Rare,
                flavor: "与开天珠并称先天双珠，可裂地成渊、陷万物于无间。");

            // ===== 先天/混沌至宝 Primordial (9品, itemTier=8, basePower=680) =====
            // 下品(544)
            yield return A("art_prm_god_slayer_spear_i", "弑神枪", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Inferior, 8, 544,
                new[] { Modules.FlatPen(80), Modules.PenFromResource("shaCharge", 8), Modules.CounterMul("god", 4, 1) },
                rarity: EffectRarity.Rare,
                flavor: "混沌至宝·弑神枪，专克神性。枪出则神明陨落。");
            yield return A("art_prm_world_grinder_i", "灭世大磨", ArtifactForm.Cauldron,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Inferior, 8, 544,
                new[] { Modules.Control("grind", 4), Modules.Dot("world_grind", 35, 4), Modules.Drain("manaPool", 10) },
                rarity: EffectRarity.Rare,
                flavor: "混沌至宝·灭世大磨，磨盘转动则天地反复、万物归墟。");
            yield return A("art_prm_universe_cauldron_i", "乾坤鼎·残", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Inferior, 8, 544,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 100, "mana上限+100"),
                    new EffectOp(EffectOpKind.AddResourceCap, "qixue", 60, "血气上限+60"),
                    new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 2, "道心+2阶") },
                rarity: EffectRarity.Rare,
                flavor: "乾坤鼎残片，可炼化万物返本归元。虽是残鼎，犹含创世之威。");
            yield return A("art_prm_chaos_lotus_frag_i", "混沌青莲·残", ArtifactForm.Lotus,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Inferior, 8, 544,
                new[] { Modules.FlatDR(70), Modules.Evade(40),
                    new EffectOp(EffectOpKind.AddResource, "qixue", 30, "回血气+30") },
                secFunc: ArtifactFunction.Heal, rarity: EffectRarity.Rare,
                flavor: "混沌青莲残瓣，防御无双且可源源不断回复生机。");

            // 中品(680)
            yield return A("art_prm_pangu_banner_c", "盘古幡", ArtifactForm.Banner,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Common, 8, 680,
                new[] { Modules.FlatPen(100), Modules.AoePerTarget(60), Modules.CounterMul("void", 5, 2) },
                rarity: EffectRarity.Rare,
                flavor: "混沌至宝中品·盘古幡，开天辟地之无上攻伐至宝。幡动混沌裂。");
            yield return A("art_prm_sky_open_axe_fake_c", "开天斧·仿", ArtifactForm.Axe,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Common, 8, 680,
                new[] { Modules.FlatPen(105), Modules.PenFromResource("qixie", 5), Modules.Backlash("burnGate", 40) },
                rarity: EffectRarity.Rare,
                flavor: "仿盘古开天斧而制，一斧开天、一击灭界。反噬极大。");
            yield return A("art_prm_taiji_scroll_c", "太极图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Common, 8, 680,
                new[] { Modules.Control("trap", 5), Modules.Dot("yin_yang_grind", 40, 4), Modules.FlatDR(45) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "混沌至宝中品·太极图，阴阳二气化金桥。可定地水风火。");
            yield return A("art_prm_chaos_bell_c", "混沌钟", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Common, 8, 680,
                new[] { Modules.FlatDR(80), Modules.Reflect(3, 4), Modules.Control("stun", 3), Modules.Evade(35) },
                rarity: EffectRarity.Rare,
                flavor: "混沌至宝中品·混沌钟，镇压鸿蒙。钟声一响，万界静止。");

            // 上品(816)
            yield return A("art_prm_jade_disc_s", "造化玉碟", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Superior, 8, 816,
                new[] { new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 4, "道心+4阶"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 120, "mana上限+120"),
                    Modules.Special("reverseStack", 5, 0, "逆演回滚·造化") },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "混沌至宝上品·造化玉碟，含三千大道。悟道者可窥天道本源。");
            yield return A("art_prm_36_chaos_lotus_s", "三十六品混沌青莲", ArtifactForm.Lotus,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Superior, 8, 816,
                new[] { Modules.FlatDR(90), Modules.Reflect(3, 4), Modules.Evade(45),
                    new EffectOp(EffectOpKind.AddResource, "qixue", 40, "回血+40") },
                secFunc: ArtifactFunction.Heal, rarity: EffectRarity.Rare,
                flavor: "三十六品混沌青莲，万法不侵、不死不灭。防御无双。");
            yield return A("art_prm_hongmeng_orb_s", "鸿蒙珠", ArtifactForm.Orb,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Superior, 8, 816,
                new[] { Modules.FlatPen(110), Modules.PenFromResource("manaPool", 3), Modules.AoePerTarget(55) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Rare,
                flavor: "混沌至宝上品·鸿蒙珠，一珠一世界。珠中演化真实宇宙。");

            // 极品(1020)
            yield return A("art_prm_open_sky_axe_m", "盘古斧", ArtifactForm.Axe,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 8, 1020,
                new[] { Modules.FlatPen(140), Modules.PenFromResource("qixie", 8), Modules.AoePerTarget(70),
                    Modules.CounterMul("void", 5, 1) },
                rarity: EffectRarity.Rare,
                flavor: "混沌至宝极品·盘古斧，开天辟地第一至宝。一斧分混沌、定乾坤。");
            yield return A("art_prm_chaos_creation_m", "混沌至宝·开天", ArtifactForm.Banner,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 8, 1020,
                new[] { Modules.FlatPen(130), Modules.AoePerTarget(65), Modules.CounterMul("void", 5, 1),
                    Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "开天辟地之威具象化的混沌至宝，攻伐与创世并存。");
            yield return A("art_prm_hongmeng_creation_m", "鸿蒙至宝·造化", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 8, 1020,
                new[] { new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 5, "道心+5阶"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 150, "mana+150"),
                    new EffectOp(EffectOpKind.AddResourceCap, "qixue", 80, "血气+80"),
                    Modules.FlatDR(50) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "鸿蒙至宝·造化，蕴含创世之初的本源大道。得之可窥造化。");
            yield return A("art_prm_great_dao_source_m", "大道本源", ArtifactForm.Orb,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 8, 1020,
                new[] { Modules.Special("reverseStack", 6, 0, "逆演·本源"),
                    new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 4, "道心+4阶"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 130, "mana+130"),
                    Modules.FlatDR(45) },
                rarity: EffectRarity.Rare,
                flavor: "大道本源·混沌至宝极品，万道归一、演化无穷。非真仙不可执。");

            // —— 补充：通天灵宝 额外 ——
            yield return A("art_hr_sky_patrol_x1", "巡天鉴", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.HeavenReaching, QualityTier.Common, 6, 340,
                new[] { Modules.CounterMul("ghost", 4, 2), Modules.FlatDR(30), Modules.Evade(20) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "巡天鉴，可查三界、监察万灵。");
            yield return A("art_hr_demon_suppress_tower_x2", "镇魔浮屠塔", ArtifactForm.Tower,
                ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Superior, 6, 408,
                new[] { Modules.Control("suppress", 4), Modules.CounterMul("evil", 3, 2), Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Rare,
                flavor: "七层浮屠镇万魔，邪魔入塔永世难出。");

            // —— 补充：玄天之宝 额外 ——
            yield return A("art_ps_blood_cauldron_x1", "血契炼天鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.ProfoundSky, QualityTier.Common, 7, 480,
                new[] { Modules.PenFromResource("qixue", 4), Modules.Drain("qixie", 8),
                    new EffectOp(EffectOpKind.AddResourceCap, "qixue", 60, "血气+60") },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Rare,
                flavor: "以血为契、炼化诸天。玄天级血炼至宝。");
            yield return A("art_ps_soul_anchor_lamp_x2", "定魂长明灯", ArtifactForm.Lamp,
                ArtifactFunction.Heal, ArtifactGrade.ProfoundSky, QualityTier.Superior, 7, 576,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 50, "回血+50"),
                    new EffectOp(EffectOpKind.AddResourceCap, "qixue", 70, "血气上限+70"),
                    Modules.FlatDR(30) },
                rarity: EffectRarity.Rare,
                flavor: "长明灯不灭，则神魂不散。大乘期最强愈宝之一。");
            yield return A("art_ps_myriad_sword_pearl_x3", "万剑归宗珠", ArtifactForm.Orb,
                ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Superior, 7, 576,
                new[] { Modules.AoePerTarget(55), Modules.PenFromResource("swordWill", 10), Modules.FlatPen(65) },
                rarity: EffectRarity.Rare,
                flavor: "一剑化万剑，万剑归宗。剑修梦寐以求的玄天之宝。");

            // —— 补充：混沌至宝 额外 ——
            yield return A("art_prm_blood_trans_mirror_x1", "血海轮回镜", ArtifactForm.Mirror,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Common, 8, 680,
                new[] { Modules.Control("blood_trap", 4), Modules.Dot("blood_corrode", 35, 3),
                    Modules.Drain("qixue", 10) },
                rarity: EffectRarity.Rare,
                flavor: "混沌至宝·血海轮回镜，镜中血海无边、入者永堕轮回。");
            yield return A("art_prm_star_river_scroll_x2", "周天星斗图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Common, 8, 680,
                new[] { Modules.Control("star_trap", 5), Modules.Dot("star_burn", 30, 4),
                    Modules.AoePerTarget(50) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Rare,
                flavor: "周天星斗大阵化为图卷，三百六十五星斗齐辉，困杀一切。");
            yield return A("art_prm_heavenly_net_orb_x3", "天网恢恢珠", ArtifactForm.Orb,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Superior, 8, 816,
                new[] { Modules.Control("net", 5), Modules.Drain("manaPool", 15), Modules.FlatDR(35) },
                rarity: EffectRarity.Rare,
                flavor: "天网恢恢疏而不漏，混沌级困宝。因果之网无可逃脱。");
        }

        // ------- 唯一档: 21路镇派+散落+遗迹 (batch4) -------
        static IEnumerable<ArtifactDef> UniqueArtifacts()
        {
            // ===== 剑修镇派 =====
            yield return A("art_unq_sword_zhu_xian", "诛仙剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("explodeArray", 9, 0, "诛仙剑意"), Modules.FlatPen(80), Modules.CounterMul("evil", 3, 1) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "诛仙四剑之首，剑修至高剑道化身。非剑心圆满者不可执。", src: "古道宗遗迹·诛仙台");
            yield return A("art_unq_sword_azure_violet", "青索紫郢双剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Supreme, 8, 960,
                new[] { Modules.PenFromResource("swordWill", 10), Modules.AoePerTarget(40) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "紫青双剑合一，剑修镇派至宝。", src: "剑修·剑阁祖传");

            // ===== 刀修镇派 =====
            yield return A("art_unq_blade_heaven_slayer", "天斩", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.PenFromResource("shaCharge", 8), Modules.FlatPen(90), Modules.AoePerTarget(50) },
                rarity: EffectRarity.Unique,
                flavor: "刀修至高天斩，一刀之下天亦两断。舍刀之外再无他物。", src: "刀修·霸刀山庄");

            // ===== 枪修镇派 =====
            yield return A("art_unq_spear_dragon_breaker", "破龙霸王枪", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatPen(95), Modules.CounterMul("dragon", 4, 1), Modules.PenFromResource("qixue", 5) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "枪修镇派至宝，一枪破龙、万军辟易。", src: "枪修·霸王枪宗");

            // ===== 器修镇派 =====
            yield return A("art_unq_qixiu_sky_tower", "混元玲珑塔", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("luobao", 5, 0, "万宝归塔"), Modules.FlatDR(60), Modules.Reflect(1, 2) },
                secFunc: ArtifactFunction.Snatch, rarity: EffectRarity.Unique,
                flavor: "器修至高杰作，可同时困+夺+防。一件至宝压一境。", src: "器修·百炼总坛");

            // ===== 阵修镇派 =====
            yield return A("art_unq_array_formation_disk", "诛仙阵图", ArtifactForm.ArrayDisk,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("explodeArray", 9, 0, "诛仙剑阵"), Modules.Control("trap", 5) },
                rarity: EffectRarity.Unique,
                flavor: "诛仙四剑配诛仙阵图，布下诛仙剑阵，圣人亦不敢轻入。", src: "阵修·古道宗遗迹");

            // ===== 体修镇派 =====
            yield return A("art_unq_body_golden_armor", "不灭金甲", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("goldenBodyMax", 3, 0, "金身不灭"), Modules.FlatDR(80), Modules.Reflect(1, 2) },
                rarity: EffectRarity.Unique,
                flavor: "体修至高金身甲，濒死复活、不灭不破。", src: "体修·横练宗祖传");

            // ===== 佛修镇派 =====
            yield return A("art_unq_bud_12_lotus", "十二品功德金莲", ArtifactForm.Lotus,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("goldenBodyMax", 5, 0, "金身不灭·佛"), Modules.FlatDR(60) },
                secFunc: ArtifactFunction.Heal, rarity: EffectRarity.Unique,
                flavor: "佛门至高莲台，万法不侵、功德无量。", src: "佛修·大日如来寺");

            // ===== 鬼修镇派 =====
            yield return A("art_unq_ghost_soul_banner", "万魂幡", ArtifactForm.Banner,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.PenFromResource("ghostSoldierPower", 5), Modules.Drain("shaCharge", 10) },
                rarity: EffectRarity.Unique,
                flavor: "收万魂于一幡，鬼兵如潮，吞天噬地。", src: "鬼修·噬魂魔宫");

            // ===== 雷修镇派 =====
            yield return A("art_unq_lei_thunder_seal", "九天应元雷声普化天尊印", ArtifactForm.Seal,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.PenFromResource("thunderCharge", 5), Modules.CounterMul("evil", 3, 1), Modules.AoePerTarget(50) },
                rarity: EffectRarity.Unique,
                flavor: "雷修至高天尊印，执掌天劫雷霆。", src: "雷修·天劫峰");

            // ===== 命修镇派 =====
            yield return A("art_unq_ming_life_death_book", "生死簿", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("reverseStack", 3, 0, "逆演回滚"), Modules.Control("fate", 5) },
                rarity: EffectRarity.Unique,
                flavor: "命修至高冥书，可定生死、判因果、逆演时空。", src: "命修·因果时空殿");

            // ===== 魔修镇派 =====
            yield return A("art_unq_mo_heart_devour_blade", "噬心魔刃", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.PenFromResource("MoGong", 5), Modules.Drain("MoGong", 10), Modules.Backlash("burnGate", 30) },
                rarity: EffectRarity.Unique,
                flavor: "以心魔淬刃，每斩必夺一魂。噬主之刃，魔修至宝。", src: "魔修·血河魔宫");

            // ===== 血修镇派 =====
            yield return A("art_unq_xue_blood_cauldron", "血神鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.PenFromResource("qixie", 5), Modules.Drain("qixie", 8), Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "以血炼鼎，燃血成神。血修至高血祭至宝。", src: "血修·血煞原祖坛");

            // ===== 音修镇派 =====
            yield return A("art_unq_yin_dragon_phoenix_qin", "龙吟凤鸣琴", ArtifactForm.Instrument,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.AoePerTarget(60), Modules.Control("stun", 4), Modules.Dot("soul_shock", 30, 3) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Unique,
                flavor: "音修至高琴器，一曲龙吟凤鸣、万灵俯首。", src: "音修·天音阁");

            // ===== 丹修镇派 =====
            yield return A("art_unq_dan_nine_revolution_cauldron", "九转金丹鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Heal, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 80, "九转回血气+80"),
                    new EffectOp(EffectOpKind.AddResourceCap, "qixue", 100, "血气上限+100"),
                    Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Support, rarity: EffectRarity.Unique,
                flavor: "丹修至高神鼎，九转金丹可起死回生。", src: "丹修·丹霞圣宗");

            // ===== 傀儡修镇派 =====
            yield return A("art_unq_kuilei_god_puppet_core", "神机百炼核心", ArtifactForm.Orb,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "bond", 30, "bond上限+30"),
                    Modules.PenFromResource("bond", 4), Modules.AoePerTarget(45) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "傀儡修至高核心，可同时操控百具神机傀儡。", src: "傀儡修·神机城");

            // ===== 法修镇派 =====
            yield return A("art_unq_fa_myriad_spell_book", "万法归宗典", ArtifactForm.Scroll,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 150, "mana上限+150"),
                    Modules.PenFromResource("manaPool", 3), Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "法修至高典籍，万法归一、言出法随。", src: "法修·万法殿");

            // ===== 毒修镇派 =====
            yield return A("art_unq_du_ten_thousand_poison_gourd", "万毒至尊葫芦", ArtifactForm.Gourd,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Dot("supreme_poison", 40, 5), Modules.Control("paralyze", 4) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "毒修至高毒宝，万毒归一、触之即亡。", src: "毒修·万毒窟");

            // ===== 冰修镇派 =====
            yield return A("art_unq_bing_absolute_zero_mirror", "玄冰封天镜", ArtifactForm.Mirror,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Control("freeze", 5), Modules.Dot("frost_bite", 30, 4), Modules.FlatDR(30) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Unique,
                flavor: "冰修至高玄冰镜，镜光所至万物冰封。", src: "冰修·极寒冰宫");

            // ===== 火修镇派 =====
            yield return A("art_unq_huo_sun_true_flame_lamp", "太阳真火灯", ArtifactForm.Lamp,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatPen(100), Modules.Dot("true_fire", 35, 4), Modules.AoePerTarget(55) },
                rarity: EffectRarity.Unique,
                flavor: "火修至高真火至宝，太阳真火燃尽万物。", src: "火修·太阳神宫");

            // ===== 风修镇派 =====
            yield return A("art_unq_feng_nine_heaven_gale_fan", "九天罡风扇", ArtifactForm.Fan,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.AoePerTarget(65), Modules.Evade(50), Modules.FlatPen(60) },
                secFunc: ArtifactFunction.Escape, rarity: EffectRarity.Unique,
                flavor: "风修至高罡风扇，一扇九天罡风起、万军飞灰。", src: "风修·罡风崖");

            // ===== 咒修镇派 =====
            yield return A("art_unq_zhou_god_curse_talisman", "神咒天书", ArtifactForm.Talisman,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Control("curse", 5), Modules.Dot("curse_decay", 35, 4), Modules.Drain("manaPool", 12) },
                rarity: EffectRarity.Unique,
                flavor: "咒修至高咒书，一言成谶、诅咒成真。", src: "咒修·咒渊");

            // ===== 江湖散落 =====
            yield return A("art_unq_world_dinghai_orb", "定海神珠", ArtifactForm.Orb,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Control("flood", 4), Modules.Dot("drown", 20, 4) },
                rarity: EffectRarity.Unique,
                flavor: "先天灵宝，二十四颗定海神珠，可演化诸天。散落于四海。", src: "江湖散落·东海遗迹");
            yield return A("art_unq_world_sky_net", "天罗地网", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Supreme, 7, 680,
                new[] { Modules.Control("net", 5) },
                rarity: EffectRarity.Unique,
                flavor: "天庭遗落之宝，可网罗天地。", src: "江湖散落·天庭废墟");
            yield return A("art_unq_world_sun_bow", "后羿射日弓", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatPen(120), Modules.CounterMul("fire", 3, 1) },
                rarity: EffectRarity.Unique,
                flavor: "后羿射日之神弓，一箭可落星辰。", src: "江湖散落·远古战场");
            yield return A("art_unq_world_immortal_slayer", "斩仙飞刀", ArtifactForm.Gourd,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("duoshe", 5, 0, "斩仙·定神"), Modules.FlatPen(85) },
                secFunc: ArtifactFunction.Snatch, rarity: EffectRarity.Unique,
                flavor: "\"请宝贝转身\"——葫芦口出白光，定住元神即斩。无物不斩。", src: "江湖散落·西昆仑");
            yield return A("art_unq_world_mountain_river_scroll", "山河社稷图", ArtifactForm.Scroll,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Control("trap", 5), Modules.Dot("world_pressure", 40, 4), Modules.FlatDR(50) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Unique,
                flavor: "山河社稷图，图中自成一界。入图者如陷真实山河，永生难出。", src: "江湖散落·女娲庙");
            yield return A("art_unq_world_yellow_tower", "天地玄黄玲珑塔", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatDR(90), Modules.Reflect(3, 4), Modules.Evade(40) },
                rarity: EffectRarity.Unique,
                flavor: "天地玄黄玲珑塔，万法不侵、功德护体。玄黄之气凝为至坚。", src: "江湖散落·三十三天");

            // ===== 遗迹出土 =====
            yield return A("art_unq_ruin_bronze_halberd", "青铜神戟", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatPen(100), Modules.CounterMul("body", 3, 1) },
                rarity: EffectRarity.Unique,
                flavor: "上古遗迹中出土的青铜神兵，刻满天铭文。", src: "遗迹出土·三星堆古战场");
            yield return A("art_unq_ruin_jade_armor", "金缕玉甲", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Supreme, 8, 960,
                new[] { Modules.FlatDR(70), Modules.Reflect(1, 3), Modules.Evade(20) },
                rarity: EffectRarity.Unique,
                flavor: "古墓出土金缕玉衣，万线织成、刀枪不入。", src: "遗迹出土·马王堆古墓");
            yield return A("art_unq_ruin_ancient_stele", "远古天书碑文", ArtifactForm.Scroll,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 5, "道心+5阶"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 100, "mana+100") },
                rarity: EffectRarity.Unique,
                flavor: "远古遗迹中发现的碑文拓片，记载失传的大道法则。", src: "遗迹出土·不周山废墟");
            yield return A("art_unq_ruin_star_hourglass", "星辰沙漏", ArtifactForm.Instrument,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("reverseStack", 4, 0, "逆演·星辰"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 80, "mana+80") },
                rarity: EffectRarity.Unique,
                flavor: "上古星相师遗物，沙漏翻转可逆转时光片刻。", src: "遗迹出土·星辰古殿");
            yield return A("art_unq_ruin_god_seal_array", "神纹阵盘", ArtifactForm.ArrayDisk,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("explodeArray", 8, 0, "神纹爆破"), Modules.Control("trap", 5), Modules.FlatDR(30) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Unique,
                flavor: "上古神纹阵盘，刻录失传的远古大阵。", src: "遗迹出土·殷墟祭坛");

            // ===== 天道奇物 =====
            yield return A("art_unq_legend_god_beating_whip", "打神鞭", ArtifactForm.Whip,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatPen(110), Modules.CounterMul("god", 5, 1), Modules.Control("stun", 3) },
                secFunc: ArtifactFunction.Trap, rarity: EffectRarity.Unique,
                flavor: "打神鞭，专打神明。一鞭出则神明辟易。", src: "天道奇物·封神台");
            yield return A("art_unq_legend_god_seal_list", "封神榜", ArtifactForm.Scroll,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "soulBond", 20, "soulBond+20"),
                    new EffectOp(EffectOpKind.AddTermWeightStep, "soulBondStep", 4, "soulBond+4阶"),
                    Modules.FlatDR(40) },
                rarity: EffectRarity.Unique,
                flavor: "封神榜，可敕封神位、掌控神权。得之可号令诸神。", src: "天道奇物·封神台");
            yield return A("art_unq_legend_he_luo_book", "河图洛书", ArtifactForm.Scroll,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("reverseStack", 5, 0, "逆演·河洛"),
                    new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 4, "道心+4阶"),
                    Modules.Evade(40) },
                secFunc: ArtifactFunction.Escape, rarity: EffectRarity.Unique,
                flavor: "河图洛书，先天八卦之源。可推演天机、逆转因果。", src: "天道奇物·黄河龙马");
            yield return A("art_unq_legend_five_element_flag", "五行旗·真", ArtifactForm.Banner,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.CounterMul("fire", 4, 2), Modules.CounterMul("ice", 4, 2),
                    Modules.CounterMul("void", 4, 2), Modules.FlatDR(45), Modules.FlatPen(60) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "五行旗真品，掌握五行生克之极致。五旗齐出，天下无敌。", src: "天道奇物·五行山");
            yield return A("art_unq_legend_sky_mend_stone", "补天石", ArtifactForm.Orb,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "qixue", 120, "血气上限+120"),
                    new EffectOp(EffectOpKind.AddResource, "qixue", 50, "回血+50"),
                    Modules.FlatDR(60) },
                secFunc: ArtifactFunction.Heal, rarity: EffectRarity.Unique,
                flavor: "女娲补天遗留的五色石，蕴含创世生机。", src: "天道奇物·不周山");
            yield return A("art_unq_legend_kunlun_mirror", "昆仑镜", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("reverseStack", 6, 0, "逆演·昆仑"),
                    Modules.Evade(60), Modules.FlatDR(35) },
                secFunc: ArtifactFunction.Escape, rarity: EffectRarity.Unique,
                flavor: "昆仑镜，可穿梭时空、逆演因果。上古天帝遗宝。", src: "天道奇物·昆仑仙境");

            // —— 补充：更多路径镇派 ——
            yield return A("art_unq_qiankun_sleeve", "乾坤袖", ArtifactForm.Rope,
                ArtifactFunction.Snatch, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("luobao", 6, 0, "袖里乾坤"), Modules.Drain("itemTier", 8) },
                rarity: EffectRarity.Unique,
                flavor: "袖里乾坤大，可纳天地万物。镇派至宝。", src: "空间道·乾坤洞天");
            yield return A("art_unq_dream_butterfly_fan", "蝶梦扇", ArtifactForm.Fan,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Control("dream", 5), Modules.Dot("dream_drain", 25, 4) },
                rarity: EffectRarity.Unique,
                flavor: "庄周梦蝶，蝶梦庄周。入梦者不知己身何在。", src: "幻修·蝶梦谷");
            yield return A("art_unq_void_devour_cauldron", "吞虚鼎", ArtifactForm.Cauldron,
                ArtifactFunction.Snatch, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Drain("manaPool", 20), Modules.Drain("qixue", 15), Modules.FlatDR(40) },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Unique,
                flavor: "吞虚鼎，可吞噬虚空、炼化万法。入鼎者法力尽失。", src: "吞噬道·虚渊");

            // —— 补充：更多散落 ——
            yield return A("art_unq_world_samsara_plate", "六道轮回盘", ArtifactForm.ArrayDisk,
                ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.Special("reverseStack", 4, 0, "轮回逆转"), Modules.Control("fate", 4) },
                rarity: EffectRarity.Unique,
                flavor: "六道轮回盘，可逆转轮回、改写命运。", src: "江湖散落·冥界之门");
            yield return A("art_unq_world_dragon_ball", "祖龙珠", ArtifactForm.Orb,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResourceCap, "qixue", 100, "血气+100"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 100, "mana+100"),
                    Modules.CounterMul("dragon", 5, 1) },
                secFunc: ArtifactFunction.Attack, rarity: EffectRarity.Unique,
                flavor: "祖龙陨落所化龙珠，蕴含祖龙全部精华。", src: "江湖散落·龙墓");
            yield return A("art_unq_world_phoenix_feather", "凤凰涅槃羽", ArtifactForm.Fan,
                ArtifactFunction.Heal, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 60, "涅槃回血+60"),
                    Modules.Special("goldenBodyMax", 2, 0, "涅槃金身") },
                secFunc: ArtifactFunction.Defense, rarity: EffectRarity.Unique,
                flavor: "凤凰涅槃时落下的尾羽，蕴含重生之力。", src: "江湖散落·梧桐神树");

            // —— 补充：更多遗迹 ——
            yield return A("art_unq_ruin_void_compass", "归墟罗盘", ArtifactForm.Instrument,
                ArtifactFunction.Escape, ArtifactGrade.ProfoundSky, QualityTier.Supreme, 8, 960,
                new[] { Modules.Evade(80), Modules.Control("space_lock", 2) },
                rarity: EffectRarity.Unique,
                flavor: "上古航海遗迹中的归墟罗盘，指针所指即为出路。", src: "遗迹出土·归墟深渊");
            yield return A("art_unq_ruin_god_bone_blade", "神骨刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { Modules.FlatPen(115), Modules.PenFromResource("shaCharge", 10),
                    Modules.Backlash("burnGate", 35) },
                rarity: EffectRarity.Unique,
                flavor: "以远古神魔之骨锻造的邪刀，每斩必噬一魂。", src: "遗迹出土·神魔战场");
            yield return A("art_unq_ruin_origin_seed", "本源种", ArtifactForm.Orb,
                ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
                new[] { new EffectOp(EffectOpKind.AddTermWeightStep, "daoHeartStep", 5, "道心+5阶"),
                    new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 150, "mana+150"),
                    new EffectOp(EffectOpKind.AddResourceCap, "qixue", 80, "血气+80") },
                rarity: EffectRarity.Unique,
                flavor: "混沌初开时遗留的创世之种，蕴含演化世界的本源力量。", src: "遗迹出土·混沌深渊");
        }

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
