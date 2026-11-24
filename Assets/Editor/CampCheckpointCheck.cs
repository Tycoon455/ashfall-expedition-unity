using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

/// <summary>自检：营地检查点快照、死亡回滚（金币/背包/装备/血量体力/关卡）。</summary>
public static class CampCheckpointCheck
{
    static int pass, fail;

    static void Check(bool ok, string label)
    {
        if (ok) { pass++; Debug.Log("[回滚自检] PASS " + label); }
        else { fail++; Debug.LogError("[回滚自检] FAIL " + label); }
    }

    public static void Run()
    {
        pass = fail = 0;
        GameData.Load();
        BindingFlags f = BindingFlags.NonPublic | BindingFlags.Instance;

        // 构造最小 GameController 环境
        GameObject host = new GameObject("CheckpointHost");
        GameController gc = host.AddComponent<GameController>();

        GameObject playerGo = new GameObject("Player");
        GameActorTag tag = playerGo.AddComponent<GameActorTag>();
        tag.professionId = "warrior";
        InventoryComponent inv = playerGo.AddComponent<InventoryComponent>();
        SetBonusComponent bonus = playerGo.AddComponent<SetBonusComponent>();
        HealthComponent hp = playerGo.AddComponent<HealthComponent>();
        StaminaComponent sta = playerGo.AddComponent<StaminaComponent>();
        PlayerController player = playerGo.AddComponent<PlayerController>();
        player.Setup("warrior", inv, bonus, 2.4f);

        GameObject levelsGo = new GameObject("Levels");
        LevelManager levels = levelsGo.AddComponent<LevelManager>();

        // 最小 overlay 环境，供 OnPlayerDied 更新文案
        GameObject overlayGo = new GameObject("Overlay");
        UnityEngine.UI.Text overlayTitle = overlayGo.AddComponent<UnityEngine.UI.Text>();
        typeof(GameController).GetField("overlay", f).SetValue(gc, overlayGo.transform);
        typeof(GameController).GetField("overlayTitle", f).SetValue(gc, overlayTitle);

        typeof(GameController).GetField("playerObj", f).SetValue(gc, playerGo);
        typeof(GameController).GetField("player", f).SetValue(gc, player);
        typeof(GameController).GetField("inventory", f).SetValue(gc, inv);
        typeof(GameController).GetField("bonus", f).SetValue(gc, bonus);
        typeof(GameController).GetField("levels", f).SetValue(gc, levels);
        typeof(GameController).GetField("ProfessionId", BindingFlags.Public | BindingFlags.Instance).SetValue(gc, "warrior");
        typeof(GameController).GetField("LevelIndex0", BindingFlags.Public | BindingFlags.Instance).SetValue(gc, 4);

        // 设置营地基线状态
        inv.Gold = 500;
        inv.Bag.Add("w_ash_watch_helmet");
        inv.Equipped[0] = "w_ash_watch_weapon";
        hp.CurrentHp = 100f;
        sta.Current = 80f;

        MethodInfo capture = typeof(GameController).GetMethod("CaptureCampCheckpoint", f);
        capture.Invoke(gc, null);

        FieldInfo cpF = typeof(GameController).GetField("campCheckpoint", f);
        CampCheckpoint cp = cpF.GetValue(gc) as CampCheckpoint;
        Check(cp != null, "拍快照生成 CampCheckpoint 对象");
        Check(cp != null && cp.gold == 500 && cp.bag.Count == 1 && cp.levelIndex0 == 4,
            "快照记录金币/背包/关卡 (500/1/4)");
        Check(cp != null && cp.equipped[0] == "w_ash_watch_weapon", "快照记录已装备栏位");
        Check(cp != null && Mathf.Approximately(cp.hp, 100f) && Mathf.Approximately(cp.stamina, 80f),
            "快照记录血量体力 (100/80)");

        // 模拟关卡内变化：打怪得金币、掉装备、掉血、换关、卖了背包物品
        inv.Gold = 9999;
        inv.Bag.Add("w_ash_watch_armor");
        inv.Bag.Clear(); // 模拟在关卡/营地卖掉或消耗了背包物品
        inv.Equipped[0] = "w_ruin_weapon_blue";
        hp.CurrentHp = 12f;
        sta.Current = 5f;
        typeof(GameController).GetField("LevelIndex0", BindingFlags.Public | BindingFlags.Instance).SetValue(gc, 5);

        MethodInfo restore = typeof(GameController).GetMethod("RestoreCampCheckpoint", f);
        restore.Invoke(gc, null);

        Check(inv.Gold == 500, "回滚后金币恢复 = 500 (实际 " + inv.Gold + ")");
        Check(inv.Bag.Count == 1 && inv.Bag[0] == "w_ash_watch_helmet", "回滚后背包恢复 (卖出/消耗的装备回归)");
        Check(inv.Equipped[0] == "w_ash_watch_weapon", "回滚后已装备栏位恢复");
        int lvl = (int)typeof(GameController).GetField("LevelIndex0", BindingFlags.Public | BindingFlags.Instance).GetValue(gc);
        Check(lvl == 4, "回滚后关卡索引回到快照关卡 (第5关营地，实际 index " + lvl + ")");
        Check(Mathf.Approximately(hp.CurrentHp, 100f), "回滚后血量恢复 = 100 (实际 " + hp.CurrentHp.ToString("F0") + ")");
        Check(Mathf.Approximately(sta.Current, 80f), "回滚后体力恢复 = 80 (实际 " + sta.Current.ToString("F0") + ")");
        GameState state = (GameState)typeof(GameController).GetField("State", BindingFlags.Public | BindingFlags.Instance).GetValue(gc);
        Check(state == GameState.Camp, "回滚后状态为营地 Camp（而非重开关卡）");
        Check(tag.professionId == "warrior", "回滚后职业一致");

        // 死亡处理设置的是 retry 动作，且 overlay 文案指向营地
        MethodInfo onDied = typeof(GameController).GetMethod("OnPlayerDied", f);
        onDied.Invoke(gc, null);
        string action = typeof(GameController).GetField("overlayButtonAction", f).GetValue(gc) as string;
        Check(action == "retry", "死亡设置回滚动作 retry");

        Object.DestroyImmediate(overlayGo);
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(levelsGo);
        Object.DestroyImmediate(host);

        string result = "[回滚自检] PASS " + pass + " / FAIL " + fail + "\n";
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/camp_checkpoint_result.txt", result);
        Debug.Log(result);
    }
}
