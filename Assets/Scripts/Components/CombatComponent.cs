using UnityEngine;

/// <summary>战斗数值容器，消耗与无敌帧由玩家控制器调用。</summary>
public class CombatComponent : MonoBehaviour
{
    /// <summary>职业战斗节奏配置。战士中距离/中攻速，游侠最远/最快，法师中距离/最慢。</summary>
    public string ProfessionId = "warrior";
    public float BaseSkillCooldown = 4f;

    public float Damage = 10f;
    public float BaseDamage = 10f;
    public float CritChance;
    public float CritMultiplier = 2f;
    public float AttackInterval = 0.48f;
    public float AttackRange = 1.5f;
    public float SkillCooldown = 4f;
    public float SkillCostMult = 1f;
    public float DodgeSpeedBonus;
    public float DodgeDistanceMult = 1f;
    public float DamageMult = 1f;
    public float DefenseMult = 1f;
    public float MoveSpeedMult = 1f;
    public float Reflect;
    public float LowHpDamage;
    public float BurnBonus;
    public int ExtraTargets;

    float iframeUntil = -1f;

    public bool IsInvulnerable { get { return Time.time < iframeUntil; } }

    /// <summary>设置职业基础节奏；必须在 ApplySetBonus 之前调用，套装修正才能叠在正确基数上。</summary>
    public void SetProfessionProfile(string professionId, float skillCooldown)
    {
        ProfessionId = string.IsNullOrEmpty(professionId) ? "warrior" : professionId;
        BaseSkillCooldown = skillCooldown;
        ApplyProfessionBase();
    }

    void ApplyProfessionBase()
    {
        switch (ProfessionId)
        {
            case "ranger":
                AttackInterval = 0.32f;  // 三职业最快
                AttackRange = 9.5f;      // 三职业最远，但仍远小于关卡宽度
                break;
            case "mage":
                AttackInterval = 0.72f;  // 三职业最慢
                AttackRange = 7f;        // 中距离
                break;
            default:
                AttackInterval = 0.5f;   // 战士中等
                AttackRange = 1.5f;      // 近战再由 PlayerController 放大 1.7 倍
                break;
        }
        SkillCooldown = BaseSkillCooldown;
    }

    public void GrantIFrame(float duration)
    {
        iframeUntil = Mathf.Max(iframeUntil, Time.time + duration);
    }

    public float IFrameFor(HealthComponent hp)
    {
        float d = GameConfig.BaseIFrame;
        if (hp != null && hp.Ratio <= GameConfig.DesperationHpThreshold)
            d += GameConfig.DesperationIFrameBonus;
        return d;
    }

    /// <summary>闪避无敌：覆盖整个冲刺过程 + 落地缓冲，低血再加额外时长。</summary>
    public float IFrameForDodge(HealthComponent hp, float dodgeDuration)
    {
        float d = dodgeDuration + GameConfig.DodgeIFramePadding;
        if (hp != null && hp.Ratio <= GameConfig.DesperationHpThreshold)
            d += GameConfig.DesperationIFrameBonus;
        return d;
    }

    public float RollDamage(HealthComponent self)
    {
        float dmg = Damage * DamageMult;
        if (self != null && self.Ratio <= GameConfig.DesperationHpThreshold)
            dmg *= 1f + LowHpDamage;
        bool crit = Random.value < CritChance;
        if (crit) dmg *= CritMultiplier;
        return dmg;
    }

    public void ApplySetBonus(SetBonusComponent bonus, InventoryComponent inv)
    {
        Damage = BaseDamage;
        CritChance = 0f;
        ApplyProfessionBase(); // 职业基数优先，再叠加套装的攻速/范围修正
        SkillCostMult = 1f;
        DamageMult = 1f;
        MoveSpeedMult = 1f;
        Reflect = 0f;
        LowHpDamage = 0f;
        DodgeDistanceMult = 1f;
        ExtraTargets = 0;

        if (inv != null)
        {
            Damage += GameData.Stat(inv.EquippedItem("weapon"), "atk");
            CritChance += GameData.Stat(inv.EquippedItem("weapon"), "crit");
        }

        if (bonus == null || bonus.ActiveSet == null) return;
        DamageMult *= bonus.Mult("attack_speed");       // 攻速套暂以伤害频率体现
        AttackInterval /= Mathf.Max(0.1f, bonus.Mult("attack_speed"));
        MoveSpeedMult *= bonus.Mult("speed_mult");
        AttackRange *= bonus.Mult("range");
        SkillCooldown *= 1f - Mathf.Clamp01(bonus.Add("skill_cdr"));
        SkillCostMult *= Mathf.Max(0.1f, bonus.Mult("skill_cost"));
        CritChance += bonus.Add("crit");
        Reflect += bonus.Add("reflect");
        LowHpDamage += bonus.Add("low_hp_damage");
        DodgeDistanceMult *= bonus.Mult("dodge_dist");
        ExtraTargets += Mathf.RoundToInt(bonus.Add("extra_targets"));
    }
}
