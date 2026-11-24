using UnityEngine;

/// <summary>
/// 开发者模式开关。对应网页版设置面板里的 DEV MODE：
/// 1) 从最高关开始  2) 金币显示 ∞ 且购买免费  3) 商店常驻本职业两套完整橙装。
/// 状态用 PlayerPrefs 持久化（等价于网页版 localStorage），刷新/重启后保留。
/// 注意：金币只改显示与扣费判断，不改 InventoryComponent.Gold 实际数值，
/// 关闭后立即恢复正常经济，存档不会被污染。
/// </summary>
public static class DevMode
{
    const string Key = "ashfall_dev_mode_v1";

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt(Key, 0) == 1; }
        set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Toggle() { Enabled = !Enabled; }

    /// <summary>HUD / 商店 / 背包统一走这里显示金币。</summary>
    public static string GoldLabel(int gold) { return Enabled ? "∞" : gold.ToString(); }

    /// <summary>开发者模式下购买不扣费。</summary>
    public static bool FreePurchase { get { return Enabled; } }
}
