using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

/// <summary>自检：小怪浮动血条、Boss 顶部血条、程序化地面纹理。</summary>
public static class GameplayVisualCheck
{
    static int pass;
    static int fail;

    static void Check(bool ok, string label)
    {
        if (ok) { pass++; Debug.Log("[视觉自检] PASS " + label); }
        else { fail++; Debug.LogError("[视觉自检] FAIL " + label); }
    }

    public static void Run()
    {
        pass = fail = 0;
        GameData.Load();
        Check(GameData.Ready, "游戏数据加载");

        // 小怪：头顶血条生成 + 掉血缩短
        GameObject enemyGo = new GameObject("CheckGrunt");
        EnemyController grunt = enemyGo.AddComponent<EnemyController>();
        grunt.Setup(GameData.Enemy("grunt"), null, 0f, GameConfig.GroundY);
        Transform bar = enemyGo.transform.Find("HPBar");
        Check(bar != null, "小怪头顶浮动血条生成");
        if (bar != null)
        {
            Transform fillT = bar.Find("Fill");
            Check(fillT != null, "血条填充对象存在");
            if (fillT != null)
            {
                float before = fillT.localScale.x;
                grunt.ApplyDamage(20f, null);
                float after = fillT.localScale.x;
                Check(before > 1f, "血条初始满值 宽=" + before.ToString("F2"));
                Check(after < before, "掉血后血条缩短 " + before.ToString("F2") + " -> " + after.ToString("F2"));
            }
            Check(bar.Find("Back") != null && bar.Find("Edge") != null, "血条底板与描边存在");
        }
        Object.DestroyImmediate(enemyGo);

        // Boss：不显示浮动血条（改走屏幕顶部）
        GameObject bossGo = new GameObject("CheckBoss");
        EnemyController boss = bossGo.AddComponent<EnemyController>();
        boss.Setup(GameData.Enemy("boss_final"), null, 0f, GameConfig.GroundY);
        Check(bossGo.transform.Find("HPBar") == null, "Boss 无浮动血条，走顶部横条");
        Object.DestroyImmediate(bossGo);

        // 地面纹理
        Sprite ground = GroundArt.Ground();
        Check(ground != null, "程序化地面精灵生成");
        if (ground != null)
        {
            Check(ground.texture.width == 128 && ground.texture.height == 48,
                "地面纹理尺寸 128x48");
            Check(Mathf.Approximately(ground.pivot.y, 0f), "地面 pivot 在底部中央");
            Check(Mathf.Approximately(ground.bounds.size.x, 3.2f) &&
                  Mathf.Approximately(ground.bounds.size.y, 1.2f),
                "地面精灵世界尺寸 3.2x1.2");
        }
        string levelSrc = File.ReadAllText("Assets/Scripts/Gameplay/LevelManager.cs");
        string campSrc = File.ReadAllText("Assets/Scripts/Gameplay/CampMap.cs");
        Check(levelSrc.Contains("SpriteDrawMode.Tiled") && levelSrc.Contains("GroundArt.Ground()"),
            "关卡地面改为平铺像素纹理");
        Check(campSrc.Contains("SpriteDrawMode.Tiled") && campSrc.Contains("GroundArt.Ground()"),
            "营地地面改为平铺像素纹理");

        // HUD 顶部 Boss 血条
        string hudSrc = File.ReadAllText("Assets/Scripts/UI/HUD.cs");
        Check(hudSrc.Contains("UpdateBossBar") && hudSrc.Contains("bossFill"),
            "HUD 含 Boss 顶部血条同步逻辑");
        GameObject canvasGo = new GameObject("CheckCanvas", typeof(RectTransform), typeof(Canvas));
        HUD hud = new GameObject("CheckHUD").AddComponent<HUD>();
        hud.Build(canvasGo.transform);
        Transform bossBar = canvasGo.transform.Find("HUD/BossBar");
        Check(bossBar != null, "HUD 构建出 BossBar 节点");
        Check(bossBar != null && !bossBar.gameObject.activeSelf, "无 Boss 时顶部血条隐藏");
        System.Reflection.MethodInfo sync = typeof(HUD).GetMethod("UpdateBossBar",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        GameObject liveBoss = new GameObject("LiveBoss");
        EnemyController miniBoss = liveBoss.AddComponent<EnemyController>();
        miniBoss.Setup(GameData.Enemy("boss_mini"), null, 0f, GameConfig.GroundY);
        if (sync != null) sync.Invoke(hud, null);
        Check(bossBar != null && bossBar.gameObject.activeSelf, "Boss 在场时顶部血条显示");
        Image fillImg = bossBar != null ? bossBar.Find("BossFill").GetComponent<Image>() : null;
        Check(fillImg != null && Mathf.Approximately(fillImg.fillAmount, 1f), "Boss 满血条满格");
        miniBoss.ApplyDamage(200f, null);
        if (sync != null) sync.Invoke(hud, null);
        Check(fillImg != null && fillImg.fillAmount < 1f, "Boss 掉血后顶部血条同步缩短");
        Object.DestroyImmediate(liveBoss);
        if (sync != null) sync.Invoke(hud, null);
        Check(bossBar != null && !bossBar.gameObject.activeSelf, "Boss 死亡后顶部血条隐藏");

        Object.DestroyImmediate(canvasGo);
        Object.DestroyImmediate(hud.gameObject);

        string result = "[视觉自检] PASS " + pass + " / FAIL " + fail + "\n";
        File.WriteAllText("F:/2Dgame Demo/unity/gameplay_visual_result.txt", result);
        Debug.Log(result);
    }
}
