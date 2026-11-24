using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>自检：商店品质权重、付费刷新固定 5 金、关卡金币倍率与通关奖励。</summary>
public static class ShopEconomyCheck
{
    static int pass, fail;

    static void Check(bool ok, string label)
    {
        if (ok) { pass++; Debug.Log("[经济自检] PASS " + label); }
        else { fail++; Debug.LogError("[经济自检] FAIL " + label); }
    }

    public static void Run()
    {
        pass = fail = 0;
        GameData.Load();
        bool origDev = DevMode.Enabled;
        DevMode.Enabled = false; // 经济断言按普通模式计算，结束前还原
        BindingFlags f = BindingFlags.NonPublic | BindingFlags.Instance;

        // --- 1. 品质权重：第9/10关营地紫为主、橙为辅 ---
        int[] w8 = GameData.LootWeight(8); // 第9关营地
        int[] w9 = GameData.LootWeight(9); // 第10关营地
        Check(w8.Length == 5 && w8[3] >= 50 && w8[4] >= 15,
            "第9关营地权重紫≥50 橙≥15 → [" + string.Join(",", w8) + "]");
        Check(w9.Length == 5 && w9[3] >= 55 && w9[4] >= 25,
            "第10关营地权重紫≥55 橙≥25 → [" + string.Join(",", w9) + "]");
        Check(w8[0] == 0 && w8[1] <= 5 && w9[0] == 0 && w9[1] == 0,
            "高档营地几乎不出灰绿装");
        int[] w0 = GameData.LootWeight(0);
        Check(w0[0] >= 60, "低关卡仍以灰装为主，难度曲线保留 → [" + string.Join(",", w0) + "]");

        // --- 1b. 商店专属品质曲线：低关灰绿为主，高关紫橙为主 ---
        int[] s0 = GameData.ShopWeight(0), s4 = GameData.ShopWeight(4), s9 = GameData.ShopWeight(9);
        Check(s0.Length == 5 && s0[0] + s0[1] >= 85 && s0[3] == 0 && s0[4] == 0,
              "第1关营地商店以灰绿为主、无紫橙 → [" + string.Join(",", s0) + "]");
        Check(s4[2] > s0[2] && s4[3] > s0[3],
              "中段营地蓝色/紫色概率高于第1关 → [" + string.Join(",", s4) + "]");
        Check(s9[3] + s9[4] >= 70 && s9[0] == 0,
              "第10关营地商店紫橙为主、无灰装 → [" + string.Join(",", s9) + "]");
        bool monotone = true;
        int prevHigh = 0;
        for (int lv = 0; lv < 10; lv++)
        {
            int[] sw = GameData.ShopWeight(lv);
            int high = sw[3] + sw[4];
            if (high < prevHigh) monotone = false;
            prevHigh = high;
        }
        Check(monotone, "紫橙概率随关卡单调不降");
        int[] lw0 = GameData.LootWeight(0);
        bool decoupled = false;
        for (int i = 0; i < 5; i++) if (s0[i] != lw0[i]) decoupled = true;
        Check(decoupled, "商店品质曲线与战斗掉落已解耦");

        // --- 2. 金币倍率随关卡提升 ---
        GameObject host = new GameObject("EconHost");
        GameController gc = host.AddComponent<GameController>();
        MethodInfo multM = typeof(GameController).GetMethod("GoldMultiplier", f);
        FieldInfo lvlF = typeof(GameController).GetField("LevelIndex0", BindingFlags.Public | BindingFlags.Instance);
        lvlF.SetValue(gc, 0);
        float mul0 = (float)multM.Invoke(gc, null);
        lvlF.SetValue(gc, 9);
        float mul9 = (float)multM.Invoke(gc, null);
        Check(Mathf.Approximately(mul0, 1f), "第1关金币倍率 = 1.0 (" + mul0.ToString("F2") + ")");
        Check(mul9 >= 3.3f && mul9 <= 3.5f, "第10关金币倍率 ≈3.4 (" + mul9.ToString("F2") + ")");
        Check(mul9 > mul0, "金币倍率随关卡单调递增");

        // --- 3. 通关奖励随关卡提升 ---
        MethodInfo clearM = typeof(GameController).GetMethod("ClearReward", f);
        int r0 = (int)clearM.Invoke(gc, new object[] { 0 });
        int r4 = (int)clearM.Invoke(gc, new object[] { 4 });
        int r9 = (int)clearM.Invoke(gc, new object[] { 9 });
        Check(r9 > r4 && r4 > r0, "通关奖励随关卡提升 (" + r0 + " < " + r4 + " < " + r9 + ")");
        Check(r9 >= 300, "第10关通关奖励可观 = " + r9);

        // --- 4. 商店刷新按钮存在、价格固定 5 金（不随次数或关卡变化） ---
        GameObject canvasGo = new GameObject("EconCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        ShopPanel shop = new GameObject("Shop").AddComponent<ShopPanel>();
        System.Action noop = () => { };
        shop.Build(canvas.transform, noop);
        Transform refresh = canvasGo.transform.Find("ShopPanel/Refresh");
        Check(refresh != null, "商店构建出刷新按钮 Refresh");

        GameObject invGo = new GameObject("Inv");
        InventoryComponent inv = invGo.AddComponent<InventoryComponent>();
        inv.Gold = 5000;
        MethodInfo openM = typeof(ShopPanel).GetMethod("Open", BindingFlags.Public | BindingFlags.Instance);
        openM.Invoke(shop, new object[] { inv, 8, "warrior", null });
        // 库存稳定性：同一批货 Hide→Open 不重掷，只有 DoRefresh 才换
        FieldInfo stockF = typeof(ShopPanel).GetField("stock", f);
        var stockList = stockF.GetValue(shop) as System.Collections.Generic.List<ItemData>;
        string before = stockList == null ? "" : string.Join(",", stockList.ConvertAll(x => x.id).ToArray());
        MethodInfo hideM = typeof(ShopPanel).GetMethod("Hide", BindingFlags.Public | BindingFlags.Instance);
        hideM.Invoke(shop, null);
        openM.Invoke(shop, new object[] { inv, 8, "warrior", null });
        stockList = stockF.GetValue(shop) as System.Collections.Generic.List<ItemData>;
        string afterReopen = stockList == null ? "" : string.Join(",", stockList.ConvertAll(x => x.id).ToArray());
        Check(before.Length > 0 && before == afterReopen, "关闭重开商店保留同一批货（不自动刷新）");

        UnityEngine.UI.Text refreshLabel = refresh != null ? refresh.GetComponentInChildren<UnityEngine.UI.Text>() : null;
        Check(refreshLabel != null && refreshLabel.text == "刷新 5 金",
              "刷新按钮显示固定价格: " + (refreshLabel == null ? "无" : refreshLabel.text));
        MethodInfo costM = typeof(ShopPanel).GetMethod("RefreshCost", f);
        FieldInfo rcF = typeof(ShopPanel).GetField("refreshCount", f);
        int c0 = (int)costM.Invoke(shop, null);
        rcF.SetValue(shop, 1);
        int c1 = (int)costM.Invoke(shop, null);
        rcF.SetValue(shop, 2);
        int c2 = (int)costM.Invoke(shop, null);
        Check(c0 == 5 && c1 == 5 && c2 == 5, "刷新价格固定 5 金 (" + c0 + "/" + c1 + "/" + c2 + ")");

        // 实际点击刷新：金币恰好减少 5、刷新次数+1
        inv.Gold = 5000;
        rcF.SetValue(shop, 0);
        int goldBefore = inv.Gold;
        MethodInfo refreshM = typeof(ShopPanel).GetMethod("DoRefresh", f);
        refreshM.Invoke(shop, null);
        Check(inv.Gold == goldBefore - 5, "刷新恰好扣 5 金 (" + goldBefore + " -> " + inv.Gold + ")");
        Check((int)rcF.GetValue(shop) == 1, "刷新后 refreshCount +1");
        stockList = stockF.GetValue(shop) as System.Collections.Generic.List<ItemData>;
        string afterRefresh = stockList == null ? "" : string.Join(",", stockList.ConvertAll(x => x.id).ToArray());
        Check(afterRefresh.Length > 0, "按刷新按钮后确实重掷了商品");

        // 价格不随关卡/次数变化：第1关首次 = 第10关第4次 = 5
        FieldInfo lvlShopF = typeof(ShopPanel).GetField("currentLevel", f);
        lvlShopF.SetValue(shop, 0);
        rcF.SetValue(shop, 0);
        int low0 = (int)costM.Invoke(shop, null);
        lvlShopF.SetValue(shop, 9);
        rcF.SetValue(shop, 3);
        int high3 = (int)costM.Invoke(shop, null);
        Check(low0 == 5 && high3 == 5, "刷新价不随关卡/次数上涨 (" + low0 + " -> " + high3 + ")");

        Object.DestroyImmediate(shop.gameObject);
        Object.DestroyImmediate(canvasGo);
        Object.DestroyImmediate(invGo);
        Object.DestroyImmediate(host);
        DevMode.Enabled = origDev;

        string result = "[经济自检] PASS " + pass + " / FAIL " + fail + "\n";
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/shop_economy_result.txt", result);
        Debug.Log(result);
    }
}
