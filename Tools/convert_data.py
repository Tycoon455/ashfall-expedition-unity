"""把 Godot 时代的字典 JSON 转成 Unity JsonUtility 可反序列化的数组 JSON。
改数值仍改源数据后重跑本脚本即可。
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets", "Resources", "Data", "game_data_source.json")
DST = os.path.join(ROOT, "Assets", "Resources", "Data", "game_database.json")


def entries(d):
    out = []
    for k, v in (d or {}).items():
        try:
            value = float(v)
        except (TypeError, ValueError):
            value = 0.0
        out.append({"key": k, "value": value})
    return out


def main():
    raw = json.load(open(SRC, encoding="utf-8"))

    professions = []
    for pid, p in raw["professions"].items():
        professions.append({
            "id": pid,
            "name": p.get("name", ""),
            "max_hp": p.get("max_hp", 100),
            "max_stamina": p.get("max_stamina", 100),
            "attack": p.get("attack", 10),
            "defense": p.get("defense", 0),
            "speed": p.get("speed", 200),
            "skill": p.get("skill", ""),
            "desc": p.get("desc", ""),
        })

    items = []
    for iid, it in raw["items"].items():
        items.append({
            "id": iid,
            "name": it.get("name", ""),
            "type": it.get("type", ""),
            "profession": it.get("profession", ""),
            "set_id": it.get("set_id", ""),
            "rarity": it.get("rarity", 0),
            "price": it.get("price", 0),
            "heal": it.get("heal", 0),
            "effect": it.get("effect", ""),
            "desc": it.get("desc", ""),
            "stats": entries(it.get("stats", {})),
        })

    sets = []
    for sid, st in raw["sets"].items():
        sets.append({
            "id": sid,
            "name": st.get("name", ""),
            "profession": st.get("profession", ""),
            "rarity": st.get("rarity", 0),
            "bonus": st.get("bonus", ""),
            "effects": entries(st.get("effects", {})),
        })

    enemies = []
    for eid, en in raw["enemies"].items():
        color = en.get("color", [0.5, 0.5, 0.5])
        enemies.append({
            "id": eid,
            "name": en.get("name", ""),
            "kind": en.get("kind", "melee"),
            "hp": en.get("hp", 30),
            "damage": en.get("damage", 8),
            "speed": en.get("speed", 80),
            "gold": en.get("gold", 5),
            "xp": en.get("xp", 5),
            "scale": en.get("scale", 1.0),
            "color": [float(c) for c in color],
        })

    levels = []
    for lv in raw["levels"]:
        levels.append({
            "name": lv.get("name", ""),
            "index": lv.get("index", 0),
            "width": lv.get("width", 2000),
            "boss_budget": lv.get("boss_budget", 0),
            "enemies": list(lv.get("enemies", [])),
        })

    loot = [{"weights": [int(x) for x in w]} for w in raw["loot_weights"]]

    db = {
        "professions": professions,
        "items": items,
        "sets": sets,
        "enemies": enemies,
        "levels": levels,
        "loot_weights": loot,
    }
    with open(DST, "w", encoding="utf-8") as f:
        json.dump(db, f, ensure_ascii=False, separators=(",", ":"))
    print("written:", DST)
    print("professions", len(professions), "items", len(items), "sets", len(sets),
          "enemies", len(enemies), "levels", len(levels))


if __name__ == "__main__":
    main()
