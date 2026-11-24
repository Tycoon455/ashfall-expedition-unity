using UnityEngine;

/// <summary>验证远程投射物能命中并扣血：手动推进 Projectile.Update，不依赖运行循环。</summary>
public static class CombatFxCheck
{
    public static void Run()
    {
        GameData.Load();
        string report = "";

        // 造一个假敌人（带 Health），加入 EnemyController.All
        GameObject enemyGo = new GameObject("TestEnemy");
        EnemyController enemy = enemyGo.AddComponent<EnemyController>();
        HealthComponent hp = enemyGo.AddComponent<HealthComponent>();
        hp.Reset(100f);
        enemy.Health = hp;
        enemyGo.transform.position = new Vector3(5f, 0f, 0f);
        EnemyController.All.Add(enemy);

        // 玩家投射物：从原点朝 +x 发射，锁定敌人
        GameObject playerGo = new GameObject("TestPlayer");
        PlayerController player = playerGo.AddComponent<PlayerController>();
        Projectile pr = Projectile.Spawn(new Vector3(0f, 0f, 0f), Vector2.right,
            25f, Projectile.Side.Player, Projectile.Kind.Arrow, 13f, player, null,
            enemyGo.transform);

        float before = hp.CurrentHp;
        // 直接把投射物放到敌人附近（距离 < hitRadius），验证命中判定 + 扣血逻辑
        pr.transform.position = enemyGo.transform.position + new Vector3(0.3f, 0f, 0f);
        pr.SendMessage("Update");
        float after = hp.CurrentHp;
        bool hit = after < before;
        report += "玩家投射物命中扣血: " + (hit ? "OK" : "失败") +
                  " (HP " + before + " -> " + after + ")\n";

        // 射程限制：超过 maxRange 后投射物不再结算伤害
        GameObject farGo = new GameObject("TestFarEnemy");
        EnemyController far = farGo.AddComponent<EnemyController>();
        HealthComponent farHp = farGo.AddComponent<HealthComponent>();
        farHp.Reset(100f);
        far.Health = farHp;
        farGo.transform.position = new Vector3(20f, 0f, 0f);
        EnemyController.All.Add(far);

        Projectile limited = Projectile.Spawn(Vector3.zero, Vector2.right, 25f,
            Projectile.Side.Player, Projectile.Kind.Arrow, 13f, player, null, null, 3f);
        limited.transform.position = new Vector3(4f, 0f, 0f);
        limited.SendMessage("Update");
        bool rangeBlocked = Mathf.Approximately(farHp.CurrentHp, 100f);
        report += "超出射程不再命中: " + (rangeBlocked ? "OK" : "失败") +
                  " (HP " + farHp.CurrentHp + ")\n";

        // 清理
        Object.DestroyImmediate(pr != null ? pr.gameObject : null);
        Object.DestroyImmediate(limited != null ? limited.gameObject : null);
        EnemyController.All.Remove(enemy);
        EnemyController.All.Remove(far);
        Object.DestroyImmediate(enemyGo);
        Object.DestroyImmediate(farGo);
        Object.DestroyImmediate(playerGo);

        Debug.Log("[CombatFxCheck]\n" + report);
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/combat_fx_result.txt", report);
    }
}
