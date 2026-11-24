using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatEntry
{
    public string key;
    public float value;
}

[Serializable]
public class ProfessionData
{
    public string id;
    public string name;
    public int max_hp;
    public int max_stamina;
    public int attack;
    public int defense;
    public float speed;
    public string skill;
    public string desc;
}

[Serializable]
public class ItemData
{
    public string id;
    public string name;
    public string type;
    public string profession;
    public string set_id;
    public int rarity;
    public int price;
    public int heal;
    public string effect;
    public string desc;
    public StatEntry[] stats;
}

[Serializable]
public class SetData
{
    public string id;
    public string name;
    public string profession;
    public int rarity;
    public string bonus;
    public StatEntry[] effects;
}

[Serializable]
public class EnemyData
{
    public string id;
    public string name;
    public string kind;
    public int hp;
    public int damage;
    public int speed;
    public int gold;
    public int xp;
    public float scale;
    public float[] color;
}

[Serializable]
public class LevelData
{
    public string name;
    public int index;
    public int width;
    public int boss_budget;
    public string[] enemies;
}

[Serializable]
public class LootWeights
{
    public int[] weights;
}

[Serializable]
public class GameDatabase
{
    public ProfessionData[] professions;
    public ItemData[] items;
    public SetData[] sets;
    public EnemyData[] enemies;
    public LevelData[] levels;
    public LootWeights[] loot_weights;
    public LootWeights[] shop_weights;
}

/// <summary>数据只读仓库：JSON 一次反序列化，后续全部走字典索引。</summary>
public static class GameData
{
    static GameDatabase db;
    static readonly Dictionary<string, ProfessionData> professions = new Dictionary<string, ProfessionData>();
    static readonly Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();
    static readonly Dictionary<string, SetData> sets = new Dictionary<string, SetData>();
    static readonly Dictionary<string, EnemyData> enemies = new Dictionary<string, EnemyData>();
    static readonly List<LevelData> levels = new List<LevelData>();
    static readonly List<int[]> lootWeights = new List<int[]>();
    static readonly List<int[]> shopWeights = new List<int[]>();

    public static bool Ready { get; private set; }

    public static void Load()
    {
        if (Ready) return;
        TextAsset asset = Resources.Load<TextAsset>("Data/game_database");
        if (asset == null)
        {
            Debug.LogError("[GameData] 找不到 Resources/Data/game_database.json");
            return;
        }
        db = JsonUtility.FromJson<GameDatabase>(asset.text);
        if (db == null)
        {
            Debug.LogError("[GameData] JSON 反序列化失败");
            return;
        }

        foreach (ProfessionData p in db.professions) professions[p.id] = p;
        foreach (ItemData i in db.items) items[i.id] = i;
        foreach (SetData s in db.sets) sets[s.id] = s;
        foreach (EnemyData e in db.enemies) enemies[e.id] = e;
        levels.AddRange(db.levels);
        foreach (LootWeights w in db.loot_weights) lootWeights.Add(w.weights);
        // 商店品质曲线独立于战斗掉落；老存档缺字段时回退到 loot_weights
        if (db.shop_weights != null)
            foreach (LootWeights w in db.shop_weights) shopWeights.Add(w.weights);

        Ready = true;
        Debug.Log($"[GameData] 载入完成：{professions.Count} 职业 / {items.Count} 物品 / {sets.Count} 套装 / {levels.Count} 关");
    }

    public static ProfessionData Profession(string id)
    {
        ProfessionData v;
        return professions.TryGetValue(id, out v) ? v : null;
    }

    public static ItemData Item(string id)
    {
        ItemData v;
        return items.TryGetValue(id, out v) ? v : null;
    }

    public static SetData Set(string id)
    {
        SetData v;
        return sets.TryGetValue(id, out v) ? v : null;
    }

    public static EnemyData Enemy(string id)
    {
        EnemyData v;
        return enemies.TryGetValue(id, out v) ? v : null;
    }

    public static LevelData Level(int index0)
    {
        if (index0 < 0 || index0 >= levels.Count) return null;
        return levels[index0];
    }

    public static int LevelCount { get { return levels.Count; } }

    public static int[] LootWeight(int levelIndex0)
    {
        int i = Mathf.Clamp(levelIndex0, 0, lootWeights.Count - 1);
        return lootWeights[i];
    }

    /// <summary>营地商店刷新的品质权重：低关以灰绿为主，逐关引入蓝紫橙。</summary>
    public static int[] ShopWeight(int levelIndex0)
    {
        if (shopWeights.Count == 0) return LootWeight(levelIndex0);
        int i = Mathf.Clamp(levelIndex0, 0, shopWeights.Count - 1);
        return shopWeights[i];
    }

    public static List<ItemData> ItemsByType(string type, string profession)
    {
        List<ItemData> result = new List<ItemData>();
        foreach (ItemData it in db.items)
        {
            if (it.type != type) continue;
            if (!string.IsNullOrEmpty(it.profession) && it.profession != profession) continue;
            result.Add(it);
        }
        return result;
    }

    public static List<ItemData> Consumables()
    {
        List<ItemData> result = new List<ItemData>();
        foreach (ItemData it in db.items)
            if (it.type == "consumable") result.Add(it);
        return result;
    }

    public static float Stat(ItemData item, string key)
    {
        if (item == null || item.stats == null) return 0f;
        foreach (StatEntry s in item.stats)
            if (s.key == key) return s.value;
        return 0f;
    }

    public static float Effect(SetData set, string key)
    {
        if (set == null || set.effects == null) return 0f;
        foreach (StatEntry s in set.effects)
            if (s.key == key) return s.value;
        return 0f;
    }
}
