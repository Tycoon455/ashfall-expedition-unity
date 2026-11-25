using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 营地独立地图（对应网页版 scene='camp'）：可自由走动，四个站点走近按 E 交互。
/// 玩家实体复用 GameController 的 PlayerController，这里只负责移动/朝向/交互与场景绘制。
/// </summary>
public class CampMap : MonoBehaviour
{
    public class Station
    {
        public string id;
        public string name;
        public string sub;
        public float x;
        public Color color;
        public string spritePath;   // 站点模型素材（Resources/Art/ 下相对路径）
        public float modelHeight;   // 期望的世界高度
        public GameObject model;    // 运行时生成的模型对象
        public SpriteRenderer glow; // 地面光晕，用于呼吸动画
        public float baseScale;     // 模型基准缩放，动画只在此基础上波动
    }

    public const float Width = 46f; // 2300px * WorldScale(0.02)
    const float NearDist = 2.4f;    // 120px * WorldScale

    public static readonly Station[] Stations =
    {
        new Station { id = "shop", name = "流浪商人", sub = "购买装备与医疗品", x = 8.6f,
            color = new Color(0.86f, 0.68f, 0.28f), spritePath = "Props/merchant", modelHeight = 5.4f },
        // 用户指定：人物状态站点的建模为篝火
        new Station { id = "hero", name = "人物状态", sub = "属性 / 装备 / 背包", x = 19.6f,
            color = new Color(0.96f, 0.58f, 0.24f), spritePath = "Props/campfire", modelHeight = 3.4f },
        new Station { id = "save", name = "记忆石碑", sub = "保存进度", x = 30.4f,
            color = new Color(0.36f, 0.72f, 0.92f), spritePath = "Props/memory_stone", modelHeight = 6.2f },
        new Station { id = "gate", name = "传送门",   sub = "进入下一关", x = 41f,
            color = new Color(0.72f, 0.42f, 0.9f), spritePath = "Props/portal", modelHeight = 4.6f }
    };

    GameController gc;
    PlayerController player;
    GameObject worldRoot;
    GameObject canvasGo;
    Text infoText;
    Text nearText;
    bool active;
    float animTime;
    int currentLevel0;

    /// <summary>按目标世界高度放置精灵模型，底部贴地，返回模型对象。</summary>
    GameObject PlaceModel(Transform parent, string name, string spritePath, float x, float height, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite sp = ResourceFactory.Sprite(spritePath, 100f);
        sr.sprite = sp;
        sr.sortingOrder = order;
        float s;
        if (sp != null)
        {
            // 素材按 100ppu 换算出的高度归一化到期望高度
            s = height / Mathf.Max(0.01f, sp.bounds.size.y);
        }
        else
        {
            Debug.LogWarning("[CampMap] 缺少站点素材 " + spritePath);
            s = 1f;
        }
        go.transform.localScale = new Vector3(s, s, 1f);
        // 精灵 pivot 居中，底部贴地：y = 地面 + 半高
        go.transform.position = new Vector3(x, GameConfig.GroundY + height * 0.5f, 0f);
        return go;
    }

    /// <summary>面板打开时挂起营地移动与交互。</summary>
    public bool Paused { get; set; }

    public void Build(Transform parent, GameController controller)
    {
        gc = controller;

        worldRoot = new GameObject("CampWorld");
        worldRoot.transform.SetParent(parent, false);
        worldRoot.SetActive(false);

        // 背景：营地废土图
        GameObject bg = new GameObject("CampBackground");
        bg.transform.SetParent(worldRoot.transform, false);
        SpriteRenderer bgr = bg.AddComponent<SpriteRenderer>();
        bgr.sprite = ResourceFactory.Sprite("Backgrounds/camp", 100f);
        float scale = Width / 15.36f; // 1536px @100ppu
        bg.transform.localScale = new Vector3(scale, scale, 1f);
        bg.transform.position = new Vector3(Width * 0.5f, 0f, 0f);
        bgr.sortingOrder = -30;

        // 地面：与关卡同款程序化像素纹理，平铺不拉伸
        GameObject ground = new GameObject("CampGround");
        ground.transform.SetParent(worldRoot.transform, false);
        SpriteRenderer gr = ground.AddComponent<SpriteRenderer>();
        gr.sprite = GroundArt.Ground();
        gr.drawMode = SpriteDrawMode.Tiled;
        gr.size = new Vector2(Width, 1.2f);
        gr.transform.localPosition = new Vector3(Width * 0.5f, GameConfig.GroundY - 1.2f, 0f);
        gr.sortingOrder = -20;

        // 四个站点：真实精灵模型 + 地面光晕
        foreach (Station st in Stations)
        {
            st.model = PlaceModel(worldRoot.transform, "Station_" + st.id,
                st.spritePath, st.x, st.modelHeight, -8);
            st.baseScale = st.model.transform.localScale.x;

            GameObject glow = new GameObject("Glow_" + st.id);
            glow.transform.SetParent(worldRoot.transform, false);
            SpriteRenderer gl = glow.AddComponent<SpriteRenderer>();
            gl.sprite = ResourceFactory.SolidSprite(new Color(st.color.r, st.color.g, st.color.b, 0.3f));
            glow.transform.localScale = new Vector3(2.6f, 0.18f, 1f);
            glow.transform.position = new Vector3(st.x, GameConfig.GroundY - 0.05f, 0f);
            gl.sortingOrder = -7;
            st.glow = gl;
        }

        // 营地专属 UI（HUD 在营地隐藏，这里单独显示信息与交互提示）
        canvasGo = new GameObject("CampUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(parent, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = UIFactory.Box(canvasGo.transform, "CampUIRoot", 0f, 0f, 1600f, 900f);
        UIFactory.Image(root, Color.clear);
        infoText = UIFactory.AddLabel(root, "Info", "", 21, new Color(0.96f, 0.86f, 0.62f),
            40f, 24f, 1100f, 44f, TextAnchor.MiddleLeft, FontStyle.Bold);
        nearText = UIFactory.AddLabel(root, "Near", "", 24, new Color(1f, 0.88f, 0.58f),
            400f, 770f, 800f, 56f, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFactory.AddLabel(root, "Help", "A/D 移动 · E 交互 · B 商店 · I 背包 · Esc 关闭面板 / 保存并回主菜单",
            16, new Color(0.86f, 0.82f, 0.74f), 40f, 846f, 1200f, 36f);

        canvasGo.SetActive(false);
        EventBus.GoldChanged += OnGoldChanged;
    }

    void OnDestroy()
    {
        EventBus.GoldChanged -= OnGoldChanged;
    }

    /// <summary>金币变化统一由事件驱动，避免营地顶栏停留在进店前的旧数值。</summary>
    void OnGoldChanged(int gold)
    {
        if (active) RefreshInfo(currentLevel0);
    }

    public void Enter(PlayerController p, int levelIndex0)
    {
        player = p;
        active = true;
        player.InputEnabled = false;
        player.transform.position = new Vector3(2.4f, GameConfig.GroundY + 1.1f, 0f);
        worldRoot.SetActive(true);
        canvasGo.SetActive(true);
        RefreshInfo(levelIndex0);
    }

    public void Exit()
    {
        active = false;
        if (worldRoot != null) worldRoot.SetActive(false);
        if (canvasGo != null) canvasGo.SetActive(false);
    }

    /// <summary>读取营地存档后恢复到石碑前的位置，而不是固定回到入口。</summary>
    public void SetPlayerX(float x)
    {
        if (player == null) return;
        Vector3 pos = player.transform.position;
        pos.x = Mathf.Clamp(x, 0.5f, Width - 0.5f);
        player.transform.position = pos;
    }

    public void RefreshInfo(int levelIndex0)
    {
        currentLevel0 = levelIndex0;
        if (infoText == null) return;
        infoText.text = "废土营地　|　第 " + (levelIndex0 + 1) + " 关 · " +
            (levelIndex0 < GameData.LevelCount ? GameData.Level(levelIndex0).name : "黑曜王座") +
            "　|　金币 " + DevMode.GoldLabel(gc == null || gc.Inventory == null ? 0 : gc.Inventory.Gold);
    }

    void Update()
    {
        if (!active) return;
        animTime += Time.deltaTime;
        // 光晕呼吸 + 篝火/传送门模型轻微律动，纯氛围不影响交互
        foreach (Station st in Stations)
        {
            if (st.glow == null) continue;
            float wave = 0.5f + 0.5f * Mathf.Sin(animTime * 3f + st.x);
            Color c = st.glow.color;
            c.a = 0.22f + wave * 0.18f;
            st.glow.color = c;
            if (st.model != null)
            {
                float breathe = (st.id == "hero" || st.id == "gate")
                    ? 1f + 0.015f * Mathf.Sin(animTime * (st.id == "hero" ? 9f : 2f))
                    : 1f;
                st.model.transform.localScale = new Vector3(st.baseScale * breathe, st.baseScale, 1f);
            }
        }

        if (Paused || player == null) return;

        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

        Vector3 pos = player.transform.position;
        pos.x = Mathf.Clamp(pos.x + h * 5f * Time.deltaTime, 0.5f, Width - 0.5f);
        player.transform.position = pos;

        // 精灵默认面朝右：facing>0 不翻转，facing<0 镜像（与网页版 flip = facing < 0 一致）
        Transform visual = player.transform.Find("Visual");
        if (visual != null && Mathf.Abs(h) > 0.01f)
        {
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * Mathf.Sign(h), Mathf.Abs(s.y), 1f);
        }

        Station near = null;
        float best = NearDist;
        foreach (Station st in Stations)
        {
            float d = Mathf.Abs(pos.x - st.x);
            if (d < best) { best = d; near = st; }
        }
        nearText.text = near == null ? "" : "[E] 交互 · " + near.name + " — " + near.sub;

        if (near != null && Input.GetKeyDown(KeyCode.E)) Interact(near.id);
        // Esc 只由 GameController.Update 统一处理：GetKeyDown 同帧在多个 Update 中都成立，
        // 这里若再监听一次，会出现"关面板后同帧又回主菜单"的连跳。
    }

    void Interact(string id)
    {
        if (id == "shop") gc.OpenShop();
        else if (id == "hero") gc.OpenBag();
        else if (id == "save") gc.OpenSaveSlots();
        else if (id == "gate") gc.NextLevel();
    }
}
