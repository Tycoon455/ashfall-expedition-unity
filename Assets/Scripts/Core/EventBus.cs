using System;
using UnityEngine;

/// <summary>轻量事件总线，UI 与玩法解耦。</summary>
public static class EventBus
{
    public static event Action<EnemyController> EnemyDied;
    public static event Action PlayerDied;
    public static event Action<int> LevelCompleted;
    public static event Action<int> GoldChanged;
    public static event Action<string> Message;
    public static event Action StateChanged;

    public static void RaiseEnemyDied(EnemyController e)
    {
        if (EnemyDied != null) EnemyDied(e);
    }

    public static void RaisePlayerDied()
    {
        if (PlayerDied != null) PlayerDied();
    }

    public static void RaiseLevelCompleted(int nextLevel)
    {
        if (LevelCompleted != null) LevelCompleted(nextLevel);
    }

    public static void RaiseGoldChanged(int gold)
    {
        if (GoldChanged != null) GoldChanged(gold);
    }

    public static void RaiseMessage(string text)
    {
        if (Message != null) Message(text);
    }

    public static void RaiseStateChanged()
    {
        if (StateChanged != null) StateChanged();
    }
}
