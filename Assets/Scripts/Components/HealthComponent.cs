using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public float MaxHp = 100f;
    public float CurrentHp;
    public float Defense;
    public int DeathSaves;
    public bool IsPlayer;

    public event Action<float, float> Changed;

    public bool Dead { get; private set; }
    public float Ratio { get { return MaxHp <= 0f ? 0f : CurrentHp / MaxHp; } }

    void Start()
    {
        if (CurrentHp <= 0f) CurrentHp = MaxHp;
        Raise();
    }

    public void Reset(float max)
    {
        MaxHp = max;
        CurrentHp = max;
        Dead = false;
        Raise();
    }

    public void TakeDamage(float amount, bool ignoreDefense = false)
    {
        if (Dead || amount <= 0f) return;
        float dmg = ignoreDefense ? amount : Mathf.Max(1f, amount - Defense);

        // 圣殿遗忠：每关一次免死
        if (DeathSaves > 0 && CurrentHp - dmg <= 0f)
        {
            DeathSaves--;
            CurrentHp = MaxHp * 0.3f;
            EventBus.RaiseMessage("遗忠守护：免除致命一击");
        }
        else
        {
            CurrentHp = Mathf.Max(0f, CurrentHp - dmg);
        }

        if (CurrentHp <= 0f)
        {
            Dead = true;
            if (IsPlayer) EventBus.RaisePlayerDied();
        }
        Raise();
    }

    public void Heal(float amount)
    {
        if (Dead || amount <= 0f) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        Raise();
    }

    void Raise()
    {
        if (Changed != null) Changed(CurrentHp, MaxHp);
    }
}
