using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>自检：击杀结算、品质权重掉落、地面掉落物生成与清理。</summary>
public static class LootDropCheck
{
    static int pass;
    static int fail;

    static void Check(bool ok, string label)
    {
        if (ok) { pass++; Debug.Log("[掉落自检] PASS " + label); }
        else { fail++; Debug.LogError("[掉落自检] FAIL " + label); }
    }

    public static void Run()
    {
        pass = fail = 0;
        GameData.Load();

        GameObject host = new GameObject("DropHost");
        GameController gc = host.AddComponent<GameController>();
        GameObject invGo = new GameObject("Inv");
        InventoryComponent inv = invGo.AddComponent<InventoryComponent>();
        BindingFlags f = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(GameController).GetField("inventory", f).SetValue(gc, inv);
        typeof(GameController).GetField("ProfessionId", BindingFlags.Public | BindingFlags.Instance)
            .SetValue(gc, "warrior");
        typeof(GameController).GetField("LevelIndex0", BindingFlags.Public | BindingFlags.Instance)
            .SetValue(gc, 4);
        MethodInfo onDied = typeof(GameController).GetMethod("OnEnemyDied", f);

        GameObject enemyGo = new GameObject("ProbeBoss");
        EnemyController boss = enemyGo.AddComponent<EnemyController>();
        boss.Setup(GameData.Enemy("boss_mini"), null, 3f, GameConfig.GroundY);

        int goldBefore = inv.Gold;
        int bagBefore = inv.Bag.Count;
        onDied.Invoke(gc, new object[] { boss });
        // 关卡倍率随 LevelIndex0 放大，击杀金与 Boss 额外奖励都乘该倍率
        MethodInfo mult = typeof(GameController).GetMethod("GoldMultiplier", f);
        float mul = (float)mult.Invoke(gc, null);
        int expectedGold = Mathf.RoundToInt(boss.Data.gold * mul) + Mathf.RoundToInt(80 * mul);
        Check(inv.Gold == goldBefore + expectedGold,
            "Boss 金币 = (基础+额外)×关卡倍率 " + mul.ToString("F2") + " (" + goldBefore + " -> " + inv.Gold + ")");
        Check(inv.Bag.Count == bagBefore + 1, "Boss 保底掉落 1 件装备");
        Check(LootDrop.All.Count >= 3, "地面生成金币/装备掉落物 (" + LootDrop.All.Count + " 个)");

        bool hasRarityOrb = false;
        foreach (LootDrop d in LootDrop.All)
        {
            SpriteRenderer orb = null;
            foreach (SpriteRenderer sr in d.GetComponentsInChildren<SpriteRenderer>())
                if (sr.gameObject.name == "Orb") orb = sr;
            if (orb != null && Mathf.Approximately(orb.color.r, GameConfig.RarityColor(3).r) &&
                Mathf.Approximately(orb.color.b, GameConfig.RarityColor(3).b))
                hasRarityOrb = true;
        }
        Check(hasRarityOrb, "装备掉落物按品质着色（紫色宝珠）");

        // 普通怪：多杀几只，验证随机装备确实会入包
        GameObject gruntGo = new GameObject("ProbeGrunt");
        EnemyController grunt = gruntGo.AddComponent<EnemyController>();
        grunt.Setup(GameData.Enemy("grunt"), null, 0f, GameConfig.GroundY);
        for (int i = 0; i < 30; i++) onDied.Invoke(gc, new object[] { grunt });
        Check(inv.Bag.Count > bagBefore + 1,
            "普通怪随机掉落生效 (30 次击杀后背包 " + inv.Bag.Count + " 件)");

        MethodInfo pickDrop = typeof(GameController).GetMethod("PickDrop", f);
        ItemData picked = pickDrop.Invoke(gc, new object[] { 3, "boss" }) as ItemData;
        Check(picked != null && picked.rarity == 3 && picked.profession == "warrior",
            "掉落池按本职业+品质筛选 (" + (picked == null ? "null" : picked.name) + ")");

        LootDrop.ClearAll();
        Check(LootDrop.All.Count == 0, "关卡清理时掉落物全部销毁");

        Object.DestroyImmediate(gruntGo);
        Object.DestroyImmediate(enemyGo);
        Object.DestroyImmediate(invGo);
        Object.DestroyImmediate(host);

        string result = "[掉落自检] PASS " + pass + " / FAIL " + fail + "\n";
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/loot_drop_result.txt", result);
        Debug.Log(result);
    }
}
