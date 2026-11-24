"""Unity 工程静态校验：C# 结构、资源引用、数据一致性、残留文件。"""
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
errors = []
checks = []


def check(condition, message):
    checks.append((bool(condition), message))
    if not condition:
        errors.append(message)


def main():
    # 1. C# 括号与基本结构
    cs_files = []
    for base, dirs, files in os.walk(os.path.join(ROOT, "Assets", "Scripts")):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(base, f)
                cs_files.append(p)
                text = open(p, encoding="utf-8").read()
                depth = 0
                for line in text.split("\n"):
                    code = line.split("//")[0]
                    depth += code.count("{") - code.count("}")
                    if depth < 0:
                        break
                check(depth == 0, f"括号未闭合: {os.path.relpath(p, ROOT)} depth={depth}")
    check(len(cs_files) >= 15, f"C# 脚本数量 {len(cs_files)} >= 15")

    all_cs = "\n".join(open(p, encoding="utf-8").read() for p in cs_files)

    # 2. ResourceFactory.Sprite 引用是否存在
    art_root = os.path.join(ROOT, "Assets", "Resources", "Art")
    sprite_paths = set(re.findall(r'ResourceFactory\.Sprite\("([^"]+)"', all_cs))
    sprite_paths |= set(re.findall(r'ResourceFactory\.Sprite\(\s*"([^"]+)"', all_cs))
    # 变量拼接路径单独按前缀检查
    dynamic_sprites = re.findall(r'ResourceFactory\.Sprite\("([^"]*?)" \+', all_cs)
    for rel in sprite_paths:
        # 动态拼接的前缀（如 "UI/frame_" + name）由下面的目录检查覆盖
        if not rel or "+" in rel or rel.endswith("_") or rel.endswith("/"):
            continue
        path = os.path.join(art_root, rel + ".png")
        check(os.path.exists(path), f"素材缺失: Resources/Art/{rel}.png")
    for prefix in dynamic_sprites:
        folder = prefix.rsplit("/", 1)[0] if "/" in prefix else ""
        full = os.path.join(art_root, folder)
        check(os.path.isdir(full), f"素材目录缺失: {folder}")

    # 3. Resources.Load 引用
    for rel in re.findall(r'Resources\.Load<[^>]+>\("([^"]+)"', all_cs):
        for ext in (".json", ".png", ".ttf", ""):
            if os.path.exists(os.path.join(ROOT, "Assets", "Resources", rel + ext)):
                break
        else:
            check(False, f"Resources 资产缺失: {rel}")

    # 4. JSON 数据一致性
    db_path = os.path.join(ROOT, "Assets", "Resources", "Data", "game_database.json")
    db = json.load(open(db_path, encoding="utf-8"))
    check(len(db["professions"]) == 3, "职业数 = 3")
    check(len(db["levels"]) == 10, "关卡数 = 10")
    check(len(db["sets"]) == 45, "套装数 = 45")
    check(len(db["items"]) == 185, "物品数 = 185")
    for prof in ("warrior", "ranger", "mage"):
        rarity_count = {}
        for s in db["sets"]:
            if s["profession"] == prof:
                rarity_count[s["rarity"]] = rarity_count.get(s["rarity"], 0) + 1
        check(all(rarity_count.get(r, 0) == r + 1 for r in range(5)),
              f"{prof} 套装品质分布 1/2/3/4/5: {rarity_count}")
    check(len(db["loot_weights"]) == 10, "掉落权重覆盖 10 关")

    # 5. 场景与 Build Settings GUID 一致
    scene_meta = open(os.path.join(ROOT, "Assets", "Scenes", "Game.unity.meta"),
                      encoding="utf-8").read()
    guid = re.search(r"guid:\s*(\w+)", scene_meta).group(1)
    build = open(os.path.join(ROOT, "ProjectSettings", "EditorBuildSettings.asset"),
                 encoding="utf-8").read()
    check(guid in build, "Build Settings 的 GUID 与场景 meta 一致")

    # 6. 关键类存在
    for cls in ["GameController", "GameBootstrap", "GameData", "SaveManager",
                "HealthComponent", "StaminaComponent", "CombatComponent",
                "InventoryComponent", "SetBonusComponent", "PlayerController",
                "EnemyController", "LevelManager", "HUD", "ShopPanel", "InventoryPanel",
                "DevMode", "CampMap", "SettingsPanel"]:
        check(re.search(r"\b(class|static class)\s+" + cls + r"\b", all_cs) is not None,
              f"类定义存在: {cls}")

    # 7. 伤势公式与关键常量
    config = open(os.path.join(ROOT, "Assets", "Scripts", "Core", "GameConfig.cs"),
                  encoding="utf-8").read()
    check("CapacityMinMult = 0.5f" in config and "CapacityHpWeight = 0.5f" in config,
          "体力上限公式常量 0.5/0.5")
    check("RegenMinMult = 0.4f" in config and "RegenHpWeight = 0.6f" in config,
          "体力回复公式常量 0.4/0.6")
    stamina = open(os.path.join(ROOT, "Assets", "Scripts", "Components", "StaminaComponent.cs"),
                   encoding="utf-8").read()
    check("GameConfig.CapacityMinMult + GameConfig.CapacityHpWeight * hpRatio" in stamina,
          "体力上限公式已实现")
    check("GameConfig.RegenMinMult + GameConfig.RegenHpWeight * hpRatio" in stamina,
          "体力回复公式已实现")

    # 7b. 网页版移植功能接线（开发者模式 + 营地地图）
    controller = open(os.path.join(ROOT, "Assets", "Scripts", "GameController.cs"),
                      encoding="utf-8").read()
    campmap = open(os.path.join(ROOT, "Assets", "Scripts", "Gameplay", "CampMap.cs"),
                   encoding="utf-8").read()
    dev = open(os.path.join(ROOT, "Assets", "Scripts", "Core", "DevMode.cs"),
               encoding="utf-8").read()
    inv = open(os.path.join(ROOT, "Assets", "Scripts", "Components", "InventoryComponent.cs"),
               encoding="utf-8").read()
    shop = open(os.path.join(ROOT, "Assets", "Scripts", "UI", "ShopPanel.cs"),
                encoding="utf-8").read()
    check("PlayerPrefs.GetInt" in dev and "public static string GoldLabel" in dev,
          "DevMode 用 PlayerPrefs 持久化并提供统一金币显示")
    check("DevMode.FreePurchase" in inv and "if (!free) Gold -= item.price" in inv,
          "开发者模式购买免费但不改实际金币数值")
    check("DevMode.Enabled" in shop and "it.rarity == 4" in shop,
          "开发者模式商店常驻橙装")
    check("DevMode.Enabled ? GameData.LevelCount - 1" in controller,
          "开发者模式新游戏直达最高关")
    for method in ["public void OpenShop()", "public void OpenBag()",
                   "public void NextLevel()", "public void ShowMenu()",
                   "public InventoryComponent Inventory"]:
        check(method in controller, "GameController 暴露给营地地图: " + method)
    for name in sorted(set(re.findall(r"\bgc\.(\w+)\(", campmap))):
        check(re.search(r"\b(?:public|private|protected)\s+(?:void|bool|int|string)\s+"
                        + name + r"\s*\(", controller) is not None,
              "CampMap 调用的 GameController." + name + " 存在")
    check("KeyCode.Escape" not in campmap and "exitKeyFrame == Time.frameCount" in controller,
          "营地 Esc 只有单一入口且同帧去重（关面板不会连跳回主菜单）")
    check(os.path.exists(os.path.join(art_root, "Backgrounds", "camp.png")),
          "营地背景素材 Resources/Art/Backgrounds/camp.png")
    check("Station[] Stations" in campmap and 'id = "gate"' in campmap,
          "营地四个交互站点已定义")

    # 8. Godot 残留
    leftovers = []
    for base, dirs, files in os.walk(ROOT):
        for f in files:
            if f.endswith((".gd", ".tscn", ".tres")) or f == "project.godot":
                leftovers.append(os.path.relpath(os.path.join(base, f), ROOT))
    check(not leftovers, f"无 Godot 文件残留: {leftovers}")

    passed = sum(1 for ok, _ in checks if ok)
    print(f"=== Unity 静态校验：{passed}/{len(checks)} 通过 ===")
    for ok, msg in checks:
        if not ok:
            print("  ✗", msg)
    if errors:
        print(f"失败 {len(errors)} 项")
        return 1
    print("全部通过")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
