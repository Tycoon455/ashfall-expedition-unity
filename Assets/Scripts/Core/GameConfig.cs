using UnityEngine;

/// <summary>全局数值常量，与 GAME_OUTLINE.md 保持一致。</summary>
public static class GameConfig
{
    // 伤势联动：体力上限 = 基础 * (0.5 + 0.5 * 血量比例)
    public const float CapacityMinMult = 0.5f;
    public const float CapacityHpWeight = 0.5f;

    // 伤势联动：体力回复 = 基础 * (0.4 + 0.6 * 血量比例)
    public const float RegenMinMult = 0.4f;
    public const float RegenHpWeight = 0.6f;

    public const float OutOfCombatDelay = 3f;
    public const float OutOfCombatRegenMult = 2f;

    // 体力消耗
    public const float StaminaAttack = 8f;
    public const float StaminaDodge = 20f;
    public const float StaminaSkill = 30f;

    // 低血保护
    public const float DesperationHpThreshold = 0.3f;
    public const float DesperationIFrameBonus = 0.08f;
    public const float BaseIFrame = 0.22f;

    // 闪避：位移更远、无敌覆盖整个闪避过程并留缓冲，避免贴脸闪避仍被打中
    public const float DodgeDuration = 0.34f;      // 闪避冲刺时长（秒）
    public const float DodgeSpeedMult = 4.4f;      // 冲刺期移动速度倍率
    public const float DodgeIFramePadding = 0.14f; // 冲刺结束后额外无敌缓冲（秒）

    public const int SaveSlotCount = 3;
    public const int InventoryCapacity = 40;

    /// <summary>数据里的像素宽度换算到 Unity 世界单位。</summary>
    public const float WorldScale = 0.02f;
    public const float GroundY = -2.4f;

    public static readonly string[] RarityNames = { "灰", "绿", "蓝", "紫", "橙" };
    public static readonly string[] RarityFiles = { "gray", "green", "blue", "purple", "orange" };

    public static readonly Color[] RarityColors =
    {
        new Color(0.62f, 0.62f, 0.62f),
        new Color(0.36f, 0.66f, 0.35f),
        new Color(0.32f, 0.58f, 0.88f),
        new Color(0.66f, 0.38f, 0.82f),
        new Color(0.98f, 0.62f, 0.18f)
    };

    public static Color RarityColor(int rarity)
    {
        int i = Mathf.Clamp(rarity, 0, RarityColors.Length - 1);
        return RarityColors[i];
    }
}
