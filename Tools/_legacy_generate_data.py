# -*- coding: utf-8 -*-
"""生成 Godot 工程 data/game_data.json。
所有静态数值集中在此，改完运行 `python tools/generate_data.py` 即可。
"""
import json
import os

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "data", "game_data.json")

RARITY_MULT = [1.0, 1.3, 1.7, 2.2, 3.0]
PRICE_MULT = [1.0, 1.8, 3.2, 6.0, 12.0]
SLOTS = ["weapon", "helmet", "armor", "amulet"]
SLOT_CN = {"weapon": "武器", "helmet": "头盔", "armor": "护甲", "amulet": "护符"}

PROFESSIONS = {
    "warrior": {"name": "战士", "max_hp": 120, "max_stamina": 100, "attack": 12, "defense": 6,
                "speed": 210, "skill": "旋风斩", "desc": "高血量近战，格挡换减伤，适合正面硬刚。"},
    "ranger":  {"name": "游侠", "max_hp": 90,  "max_stamina": 120, "attack": 10, "defense": 3,
                "speed": 240, "skill": "三连射", "desc": "高体力远程，靠闪避拉扯，持续输出。"},
    "mage":    {"name": "法师", "max_hp": 70,  "max_stamina": 90,  "attack": 16, "defense": 2,
                "speed": 220, "skill": "冰环", "desc": "低血量爆发，技能控场，容错率最低。"},
}

# (id, 套装名, 品质, 专属武器, 增益描述, effects)
SETS = {
    "warrior": [
        ("w_ash_watch",   "灰烬守夜人", 0, "锈蚀宽刃",   "格挡体力消耗 −30%；格挡成功返还 3 体力", {"block_cost_mult": 0.7, "block_refund": 3}),
        ("w_wasteland",   "荒原行军",   1, "行军短斧",   "移速 +15%；闪避后 1 秒内再 +20% 移速", {"speed_mult": 1.15, "dodge_speed_bonus": 0.2}),
        ("w_rust_sentry", "锈铁哨兵",   1, "哨兵长枪",   "正面受击减伤 15%；格挡时不再后仰", {"front_defense": 0.15, "block_no_stagger": 1}),
        ("w_iron_oath",   "铁誓重锤",   2, "铁誓战锤",   "重击附加 0.4 秒眩晕", {"stun_on_heavy": 0.4}),
        ("w_stonebreak",  "裂石开山",   2, "裂石巨斧",   "第三段普攻伤害 +50%，击退 +1 米", {"third_hit_mult": 1.5, "knockback": 1}),
        ("w_ash_guard",   "灰烬卫士",   2, "卫士塔盾",   "举盾移速 −40% 但减伤 50%，体力消耗减半", {"guard_speed_mult": 0.6, "guard_damage_mult": 0.5}),
        ("w_rustking",    "锈王残铠",   3, "锈王断刃",   "受击反弹 25%；HP<40% 时伤害 +25%", {"reflect": 0.25, "low_hp_damage": 0.25}),
        ("w_bulwark",     "断壁守望",   3, "守望长戟",   "静止 1 秒后减伤 20%、增伤 15%", {"still_defense": 0.2, "still_attack": 0.15}),
        ("w_bloodoath",   "血誓狂徒",   3, "血誓双刃",   "攻速 +25%，自身受伤 +15%", {"attack_speed": 1.25, "self_damage": 0.15}),
        ("w_ironvow",     "沉铁誓约",   3, "沉铁重锤",   "攻击不会被打断；重击击退翻倍", {"uninterruptible": 1, "heavy_knockback": 2}),
        ("w_temple",      "圣殿遗忠",   4, "遗忠圣剑",   "每关一次致命伤害保留 1 HP，并获 3 秒 50% 减伤", {"deathsave": 1, "deathsave_dr": 0.5}),
        ("w_ashdoom",     "灰烬终焉",   4, "终焉巨斧",   "对 HP<50% 的敌人伤害 ×2", {"execute_mult": 2.0}),
        ("w_iron throne", "铁血王座",   4, "王座裁决",   "击杀后 3 秒内伤害 +40%、移速 +15%", {"kill_attack": 0.4, "kill_speed": 0.15}),
        ("w_conqueror",   "荒原征服者", 4, "征服者战锤", "重击附带范围震波，可同时击退多名敌人", {"shockwave": 1}),
        ("w_immortal",    "不朽守誓人", 4, "不朽壁垒",   "体力上限 +30%；格挡成功回复 8 体力", {"max_stamina": 1.3, "block_refund": 8}),
    ],
    "ranger": [
        ("r_salvager",    "林间拾荒者", 0, "裂纹短弓",   "射速 +10%；箭矢可穿透 1 个目标", {"attack_speed": 1.1, "pierce": 1}),
        ("r_windhunt",    "风蚀游猎",   1, "风蚀猎弓",   "射速 +25%；连续命中同一目标伤害递增至 +30%", {"attack_speed": 1.25, "stack_damage": 0.3}),
        ("r_courier",     "荒野信使",   1, "信使轻弩",   "移速 +15%；闪避距离 +15%", {"speed_mult": 1.15, "dodge_dist": 1.15}),
        ("r_brokenstring","断弦誓约",   2, "断弦长弓",   "暴击率 +15%；对精英与 Boss 暴击伤害 +50%", {"crit": 0.15, "boss_crit": 0.5}),
        ("r_ashtracker",  "灰烬追踪者", 2, "追踪者强弩", "闪避留下灰烬区，敌人经过减速 40% 并持续受伤", {"ash_trail": 1, "trail_slow": 0.4}),
        ("r_duskwood",    "暮色巡林",   2, "暮色短弓",   "背击伤害 +30%；每 5 秒一次背击闪避免费", {"backstab": 0.3, "free_dodge": 1}),
        ("r_owl",         "夜枭之眼",   3, "夜枭之弓",   "射程 +40%；距离越远伤害越高，最多 +35%", {"range": 1.4, "distance_damage": 0.35}),
        ("r_thorn",       "毒棘猎手",   3, "毒棘弩",     "命中附加中毒，持续 4 秒", {"poison": 4}),
        ("r_gale",        "疾风游侠",   3, "疾风双弓",   "闪避后 2 秒内射速 +40%", {"dodge_attack_speed": 0.4}),
        ("r_cliff",       "断崖狙击",   3, "断崖重弩",   "蓄力射击伤害 +80%，蓄力时移速归零", {"charge_damage": 0.8}),
        ("r_wasteland",   "废土信使",   4, "疾风长弓",   "闪避距离 +35%；体力回复 +20%", {"dodge_dist": 1.35, "stamina_regen": 0.2}),
        ("r_sky",         "苍穹猎手",   4, "苍穹之弓",   "空中射击伤害 +50%；跳跃时射速 +20%", {"air_damage": 0.5, "air_attack_speed": 0.2}),
        ("r_shadow",      "影迹无踪",   4, "影迹短弩",   "闪避后隐身 1 秒，隐身中首击必暴击", {"dodge_stealth": 1, "stealth_crit": 1}),
        ("r_king",        "荒原游猎王", 4, "游猎王长弓", "命中同一目标伤害递增至 +50%，切换目标重置", {"stack_damage": 0.5}),
        ("r_endarrow",    "终末箭矢",   4, "终末之弓",   "命中积攒终末，满 10 层下一箭造成真实伤害", {"true_damage": 10}),
    ],
    "mage": [
        ("m_apprentice",  "余烬学徒",   0, "裂纹木杖",   "技能体力消耗 −10%；普攻附带微弱火焰", {"skill_cost": 0.9, "burn": 1}),
        ("m_tower",       "荒塔学徒",   1, "荒塔短杖",   "技能范围 +15%；普攻射程 +20%", {"range": 1.2, "skill_range": 1.15}),
        ("m_stargazer",   "静默观星者", 1, "观星法杖",   "技能体力消耗 −15%；脱战后首次技能消耗减半", {"skill_cost": 0.85, "first_free": 0.5}),
        ("m_voidscribe",  "虚空抄写员", 2, "抄写员法杖", "技能体力消耗 −30%；技能冷却 −15%", {"skill_cost": 0.7, "skill_cdr": 0.15}),
        ("m_frostvow",    "霜誓遗徒",   2, "霜誓冰杖",   "冰环冻结时间 +50%；被冻结目标受伤 +20%", {"freeze_duration": 1.5, "frozen_damage": 0.2}),
        ("m_embersage",   "余烬哲人",   2, "余烬火杖",   "火球命中附加灼烧，持续 3 秒", {"burn": 3}),
        ("m_observer",    "荒塔观测者", 3, "观测者星杖", "技能范围 +35%；技能可同时命中更多目标", {"skill_range": 1.35, "extra_targets": 2}),
        ("m_martyr",      "灰烬殉道者", 3, "殉道者骨杖", "HP<50% 时法伤 +60%，但自身受伤 +20%", {"low_hp_damage": 0.6, "self_damage": 0.2}),
        ("m_abyssbook",   "深渊抄本",   3, "深渊魔导书", "技能命中回复 5 体力；技能冷却 −10%", {"skill_refund": 5, "skill_cdr": 0.1}),
        ("m_frostsinger", "霜寒咏者",   3, "霜寒权杖",   "被冻结目标受到的伤害额外 +30%", {"frozen_damage": 0.3}),
        ("m_voidlord",    "虚空主宰",   4, "虚空法杖",   "技能冷却 −40%，体力消耗 −30%", {"skill_cdr": 0.4, "skill_cost": 0.7}),
        ("m_emberfire",   "余烬焚天",   4, "焚天火杖",   "灼烧伤害 ×2 且可叠加 3 层", {"burn_mult": 2, "burn_stack": 3}),
        ("m_towerlord",   "荒塔之主",   4, "荒塔权杖",   "技能范围 +60%；技能可穿透障碍", {"skill_range": 1.6, "pierce_obstacle": 1}),
        ("m_prophet",     "末日先知",   4, "先知预言书", "每关一次：下一次技能必定暴击且伤害 ×3", {"guaranteed_crit": 1, "crit_mult": 3}),
        ("m_abysssong",   "深渊咏叹",   4, "深渊咏叹杖", "HP<50% 时法伤 +80%、受伤 −15%", {"low_hp_damage": 0.8, "low_hp_defense": 0.15}),
    ],
}

CONSUMABLES = [
    ("med_bandage", "破旧绷带", 0, 5, 15, "无"),
    ("med_herb",    "草药膏",   1, 12, 30, "无"),
    ("med_purify",  "净化药水", 2, 25, 60, "解除中毒"),
    ("med_knight",  "骑士药剂", 3, 50, 110, "5 秒内受伤 −15%"),
    ("med_relic",   "圣遗物之血", 4, 100, 999, "回满并 5 秒减伤 30%"),
]

ENEMIES = {
    "grunt":   {"name": "腐化兵",   "hp": 40,  "damage": 8,  "speed": 80,  "gold": 8,  "xp": 10, "color": [0.55, 0.52, 0.45], "scale": 1.0, "kind": "melee"},
    "charger": {"name": "冲撞兽",   "hp": 55,  "damage": 12, "speed": 150, "gold": 12, "xp": 15, "color": [0.62, 0.40, 0.30], "scale": 1.1, "kind": "charger"},
    "ranged":  {"name": "腐矢手",   "hp": 35,  "damage": 10, "speed": 70,  "gold": 14, "xp": 15, "color": [0.40, 0.52, 0.42], "scale": 0.95, "kind": "ranged"},
    "bomber":  {"name": "孢子爆囊", "hp": 30,  "damage": 22, "speed": 100, "gold": 12, "xp": 14, "color": [0.58, 0.62, 0.30], "scale": 0.9, "kind": "bomber"},
    "shield":  {"name": "持盾残兵", "hp": 90,  "damage": 10, "speed": 60,  "gold": 20, "xp": 22, "color": [0.42, 0.44, 0.48], "scale": 1.2, "kind": "shield"},
    "elite":   {"name": "废土骑士", "hp": 180, "damage": 20, "speed": 95,  "gold": 45, "xp": 45, "color": [0.50, 0.30, 0.25], "scale": 1.4, "kind": "elite"},
    "boss_mini": {"name": "石甲守卫", "hp": 650, "damage": 26, "speed": 85, "gold": 120, "xp": 120, "color": [0.45, 0.38, 0.32], "scale": 2.0, "kind": "boss"},
    "boss_final": {"name": "深渊领主", "hp": 1600, "damage": 38, "speed": 100, "gold": 300, "xp": 300, "color": [0.35, 0.20, 0.38], "scale": 2.4, "kind": "boss"},
}

LEVELS = [
    ("新手林地",   ["grunt", "grunt", "grunt"], 2200, 0),
    ("碎石小径",   ["grunt", "charger", "grunt", "charger"], 2400, 0),
    ("腐沼边缘",   ["grunt", "ranged", "grunt", "ranged", "bomber", "grunt"], 2600, 0),
    ("废弃哨站",   ["shield", "grunt", "ranged", "elite"], 2800, 1),
    ("裂谷石桥",   ["grunt", "charger", "boss_mini"], 2600, 2),
    ("幽暗矿洞",   ["grunt", "bomber", "grunt", "ranged", "charger"], 3000, 0),
    ("孢子林",     ["bomber", "bomber", "ranged", "bomber", "grunt", "shield"], 3000, 0),
    ("霜寒高地",   ["ranged", "ranged", "elite", "grunt", "shield", "elite"], 3200, 1),
    ("王城废墟",   ["grunt", "shield", "ranged", "elite", "bomber", "charger", "elite"], 3400, 1),
    ("黑曜王座",   ["shield", "elite", "boss_final"], 3000, 3),
]

LOOT_WEIGHTS = [
    [70, 25, 5, 0, 0],
    [70, 25, 5, 0, 0],
    [40, 40, 18, 2, 0],
    [40, 40, 18, 2, 0],
    [15, 40, 35, 10, 0],
    [15, 40, 35, 10, 0],
    [5, 20, 45, 27, 3],
    [5, 20, 45, 27, 3],
    [0, 10, 35, 40, 15],
    [0, 10, 35, 40, 15],
]

BASE_STATS = {
    "weapon": {"atk": 6, "def": 0, "hp": 0, "stamina": 0, "crit": 0.01, "price": 30},
    "helmet": {"atk": 1, "def": 3, "hp": 4,  "stamina": 0, "crit": 0,    "price": 20},
    "armor":  {"atk": 0, "def": 6, "hp": 10, "stamina": 0, "crit": 0,    "price": 35},
    "amulet": {"atk": 2, "def": 1, "hp": 6,  "stamina": 6, "crit": 0.02, "price": 25},
}


def build():
    items = {}
    sets = {}

    for consumable in CONSUMABLES:
        cid, name, rarity, price, heal, effect = consumable
        items[cid] = {
            "name": name, "type": "consumable", "profession": "", "set_id": "",
            "rarity": rarity, "price": price, "heal": heal, "effect": effect,
            "stats": {}, "desc": "回复 %d 点生命。%s" % (heal, effect),
        }

    for prof, entries in SETS.items():
        for sid, set_name, rarity, weapon_name, bonus, effects in entries:
            sets[sid] = {
                "name": set_name, "profession": prof, "rarity": rarity,
                "bonus": bonus, "effects": effects,
            }
            for slot in SLOTS:
                base = BASE_STATS[slot]
                mult = RARITY_MULT[rarity]
                item_id = "%s_%s" % (sid, slot)
                display = weapon_name if slot == "weapon" else "%s·%s" % (set_name, SLOT_CN[slot])
                items[item_id] = {
                    "name": display,
                    "type": slot,
                    "profession": prof,
                    "set_id": sid,
                    "rarity": rarity,
                    "price": int(round(base["price"] * PRICE_MULT[rarity])),
                    "heal": 0,
                    "effect": "",
                    "stats": {
                        "atk": round(base["atk"] * mult, 2),
                        "def": round(base["def"] * mult, 2),
                        "hp": round(base["hp"] * mult, 2),
                        "stamina": round(base["stamina"] * mult, 2),
                        "crit": round(base["crit"] * mult, 4),
                    },
                    "desc": bonus,
                }

    level_data = []
    for i, (name, enemies, width, boss_budget) in enumerate(LEVELS):
        level_data.append({
            "name": name,
            "index": i,
            "enemies": enemies,
            "width": width,
            "boss_budget": boss_budget,
        })

    payload = {
        "professions": PROFESSIONS,
        "items": items,
        "sets": sets,
        "enemies": ENEMIES,
        "levels": level_data,
        "loot_weights": LOOT_WEIGHTS,
    }

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)
    print("generated: %s" % OUT)
    print("items=%d sets=%d levels=%d" % (len(items), len(sets), len(level_data)))
    for prof in SETS:
        print("  %s sets=%d" % (prof, len(SETS[prof])))


if __name__ == "__main__":
    build()
