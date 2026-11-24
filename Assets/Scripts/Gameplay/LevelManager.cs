using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public LevelData Current { get; private set; }
    public int LevelIndex { get; private set; }
    public float WorldWidth { get; private set; }

    readonly List<GameObject> built = new List<GameObject>();
    PlayerController player;
    InventoryComponent inventory;
    SetBonusComponent bonus;
    GameObject exitMarker;
    GameObject backdrop;
    bool exitReady;
    float exitBaseScale;
    SpriteRenderer exitGlow;
    float animTime;

    public void Build(int index0, PlayerController p, InventoryComponent inv, SetBonusComponent b)
    {
        Clear();
        player = p;
        inventory = inv;
        bonus = b;
        LevelIndex = index0;
        Current = GameData.Level(index0);
        if (Current == null)
        {
            Debug.LogError("[LevelManager] 关卡索引越界: " + index0);
            return;
        }

        WorldWidth = Current.width * GameConfig.WorldScale;
        float floor = GameConfig.GroundY;

        // 背景：
        Sprite bg = ResourceFactory.Sprite("Backgrounds/wasteland", 100f);
        if (bg != null)
        {
            GameObject go = new GameObject("Backdrop");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = bg;
            sr.sortingOrder = -20;
            // 1536x1024 @100PPU = 15.36x10.24 单位；放大到 1.2 覆盖视野高度，随摄像机水平滚动
            go.transform.position = new Vector3(1.5f, 0.6f, 2f);
            go.transform.localScale = Vector3.one * 1.2f;
            backdrop = go;
            built.Add(go);
        }
        else backdrop = null;

        // 地面：程序化像素纹理平铺（碎石沿 + 泥土裂纹），不再是一整块纯色矩形
        GameObject ground = new GameObject("Ground");
        SpriteRenderer gr = ground.AddComponent<SpriteRenderer>();
        gr.sprite = GroundArt.Ground();
        gr.drawMode = SpriteDrawMode.Tiled;
        gr.size = new Vector2(WorldWidth + 8f, 1.2f);
        gr.sortingOrder = -5;
        // 精灵 pivot 在底部中央：底边落在 floor-1.2，顶沿刚好是人物站立的 floor
        ground.transform.position = new Vector3(WorldWidth * 0.5f, floor - 1.2f, 0f);
        built.Add(ground);

        // 出生与敌人
        player.SetLevelWidth(WorldWidth);
        player.transform.position = new Vector3(1.5f, floor + 1.1f, 0f);
        float spacing = Mathf.Max(3f, (WorldWidth - 12f) / (Current.enemies.Length + 1));
        for (int i = 0; i < Current.enemies.Length; i++)
        {
            EnemyData data = GameData.Enemy(Current.enemies[i]);
            if (data == null) continue;
            GameObject go = new GameObject("Enemy_" + data.id + "_" + i);
            EnemyController enemy = go.AddComponent<EnemyController>();
            enemy.Setup(data, player, 6f + i * spacing, floor);
        }

        // 出口：金色传送门模型（清场后出现），区别于营地的紫色传送门
        exitMarker = new GameObject("Exit");
        SpriteRenderer er = exitMarker.AddComponent<SpriteRenderer>();
        Sprite exitSprite = ResourceFactory.Sprite("Props/exit_portal", 100f);
        er.sprite = exitSprite;
        er.sortingOrder = -2;
        const float exitHeight = 3.6f; // 期望世界高度
        float es = exitSprite != null
            ? exitHeight / Mathf.Max(0.01f, exitSprite.bounds.size.y)
            : 1f;
        exitBaseScale = es;
        exitMarker.transform.localScale = new Vector3(es, es, 1f);
        // 精灵 pivot 居中，底部贴地
        exitMarker.transform.position = new Vector3(WorldWidth - 1.5f, floor + exitHeight * 0.5f, 0f);
        exitMarker.SetActive(false);
        built.Add(exitMarker);

        // 出口地面光晕（清场后随出口一起显示）
        GameObject glowGo = new GameObject("ExitGlow");
        SpriteRenderer glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = ResourceFactory.SolidSprite(new Color(0.96f, 0.7f, 0.28f, 0.3f));
        glowGo.transform.localScale = new Vector3(2.8f, 0.2f, 1f);
        glowGo.transform.position = new Vector3(WorldWidth - 1.5f, floor - 0.05f, 0f);
        glowSr.sortingOrder = -3;
        glowGo.SetActive(false);
        built.Add(glowGo);
        exitGlow = glowSr;

        exitReady = false;

        EventBus.RaiseMessage("第 " + (LevelIndex + 1) + " 关 · " + Current.name);
    }

    void Update()
    {
        if (Current == null || player == null) return;
        animTime += Time.deltaTime;
        // 出口显现后：传送门轻微律动 + 地面光晕呼吸
        if (exitReady && exitMarker != null)
        {
            float breathe = 1f + 0.02f * Mathf.Sin(animTime * 2.2f);
            exitMarker.transform.localScale = new Vector3(exitBaseScale * breathe, exitBaseScale, 1f);
            if (exitGlow != null)
            {
                Color c = exitGlow.color;
                c.a = 0.22f + (0.5f + 0.5f * Mathf.Sin(animTime * 3f)) * 0.16f;
                exitGlow.color = c;
            }
        }
        // 背景作为视差远景，水平跟随摄像机，保证任意位置都有场景
        if (backdrop != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 bp = backdrop.transform.position;
                bp.x = cam.transform.position.x;
                backdrop.transform.position = bp;
            }
        }
        if (exitReady) return;
        for (int i = 0; i < EnemyController.All.Count; i++)
        {
            if (EnemyController.All[i] != null && !EnemyController.All[i].Dead) return;
        }
        exitReady = true;
        if (exitMarker != null) exitMarker.SetActive(true);
        if (exitGlow != null) exitGlow.gameObject.SetActive(true);
        EventBus.RaiseMessage("清场完成，金色传送门已开启 · 走到右侧传送门前按 E 进入");
    }

    public bool PlayerAtExit()
    {
        return exitReady && player != null &&
               player.transform.position.x >= WorldWidth - 2.2f;
    }

    public void Clear()
    {
        foreach (GameObject go in built)
            if (go != null) Destroy(go);
        built.Clear();
        for (int i = EnemyController.All.Count - 1; i >= 0; i--)
            if (EnemyController.All[i] != null) Destroy(EnemyController.All[i].gameObject);
        EnemyController.All.Clear();
        LootDrop.ClearAll();
        exitMarker = null;
        exitGlow = null;
        backdrop = null;
        exitReady = false;
        animTime = 0f;
    }
}
