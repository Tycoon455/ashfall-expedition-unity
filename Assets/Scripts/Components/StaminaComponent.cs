using UnityEngine;

/// <summary>伤势联动核心：血量比例实时压低体力上限与回复速度。</summary>
public class StaminaComponent : MonoBehaviour
{
    public float BaseMax = 100f;
    public float BaseRegen = 15f;
    public float Current;

    [HideInInspector] public float BonusMax;
    [HideInInspector] public float RegenMult = 1f;

    public float Max { get; private set; }
    public float Regen { get; private set; }
    public float Ratio { get { return Max <= 0f ? 0f : Current / Max; } }

    HealthComponent health;
    float lastCombatTime = -999f;

    public void Configure(HealthComponent hp)
    {
        health = hp;
        if (health != null) health.Changed += OnHealthChanged;
        Recalculate();
        Current = Max;
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= OnHealthChanged;
    }

    void OnHealthChanged(float cur, float max)
    {
        Recalculate();
    }

    public void Recalculate()
    {
        float hpRatio = health == null ? 1f : health.Ratio;
        Max = (BaseMax + BonusMax) *
              (GameConfig.CapacityMinMult + GameConfig.CapacityHpWeight * hpRatio);
        Regen = BaseRegen *
                (GameConfig.RegenMinMult + GameConfig.RegenHpWeight * hpRatio) * RegenMult;
        Current = Mathf.Min(Current, Max);
    }

    void Update()
    {
        float mult = (Time.time - lastCombatTime > GameConfig.OutOfCombatDelay)
            ? GameConfig.OutOfCombatRegenMult : 1f;
        Current = Mathf.Min(Max, Current + Regen * mult * Time.deltaTime);
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (Current + 0.001f < amount) return false;
        Current -= amount;
        lastCombatTime = Time.time;
        return true;
    }

    public void MarkCombat()
    {
        lastCombatTime = Time.time;
    }

    public void Refill()
    {
        Recalculate();
        Current = Max;
    }

    public void RestoreFullBase()
    {
        Recalculate();
        Current = Max;
    }
}
