using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public System.Action OnSettings;

    RectTransform root;
    BarWidget hpBar;
    BarWidget staminaBar;
    BarWidget bossBar;
    Text levelText;
    Text goldText;
    Text devBadge;
    Text messageText;
    Image injuryTag;
    Text injuryText;
    RectTransform bossRoot;
    Text bossName;
    Text bossHpText;
    RectTransform exitPromptRoot;
    Text exitPrompt;
    float messageUntil;

    /// <summary>
    /// 分层血条/体力条：底槽 + 光晕 + 左锚点填充 + 高光 + 金属边框 + 刻度 + 数值。
    /// 填充用 RectTransform 宽度而非 Image.fillAmount，方便与怪物头顶条一致地从左端收缩/增长。
    /// </summary>
    class BarWidget
    {
        RectTransform fill;
        RectTransform shine;
        RectTransform glow;
        Image fillImage;
        Image shineImage;
        Image glowImage;
        Text label;
        float innerW;
        float innerH;
        float shown = 1f;
        float target = 1f;

        public BarWidget(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform bar = UIFactory.Box(parent, name, x, y, w, h);
            UIFactory.Image(bar, Color.clear);

            // 内凹底槽
            RectTransform track = UIFactory.Box(bar, "Track", 3f, 3f, w - 6f, h - 6f);
            UIFactory.Image(track, new Color(0.055f, 0.05f, 0.055f, 0.96f));
            RectTransform shade = UIFactory.Box(bar, "Shade", 4f, 4f, w - 8f, h * 0.36f);
            UIFactory.Image(shade, new Color(0f, 0f, 0f, 0.24f));

            innerW = w - 10f;
            innerH = h - 10f;

            glow = UIFactory.Box(bar, "Glow", 4f, 1f, innerW, h - 2f);
            glowImage = UIFactory.Image(glow, new Color(1f, 0.3f, 0.2f, 0.22f));

            fill = UIFactory.Box(bar, "Fill", 5f, 5f, innerW, innerH);
            fillImage = UIFactory.Image(fill, new Color(0.8f, 0.18f, 0.14f));

            shine = UIFactory.Box(bar, "Shine", 5f, 6f, innerW, innerH * 0.32f);
            shineImage = UIFactory.Image(shine, new Color(1f, 1f, 1f, 0.22f));

            // 金属边框：上亮下暗，左右包边
            Color top = new Color(0.92f, 0.78f, 0.45f, 0.9f);
            Color bottom = new Color(0.28f, 0.22f, 0.14f, 0.95f);
            UIFactory.Image(UIFactory.Box(bar, "FrameTop", 0f, 0f, w, 3f), top);
            UIFactory.Image(UIFactory.Box(bar, "FrameBottom", 0f, h - 3f, w, 3f), bottom);
            UIFactory.Image(UIFactory.Box(bar, "FrameLeft", 0f, 0f, 4f, h), top);
            UIFactory.Image(UIFactory.Box(bar, "FrameRight", w - 4f, 0f, 4f, h), bottom);

            // 25% / 50% / 75% 刻度
            for (int i = 1; i <= 3; i++)
            {
                float tx = 5f + innerW * i / 4f - 1f;
                UIFactory.Image(UIFactory.Box(bar, "Tick" + i, tx, 5f, 2f, innerH),
                    new Color(0.05f, 0.04f, 0.03f, 0.32f));
            }

            label = UIFactory.AddLabel(bar, "Value", "", 14, Color.white,
                0f, 0f, w, h, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        public void SetColors(Color fillColor, Color glowColor)
        {
            fillImage.color = fillColor;
            Color shineColor = Color.Lerp(fillColor, Color.white, 0.6f);
            shineImage.color = new Color(shineColor.r, shineColor.g, shineColor.b, 0.28f);
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.26f);
        }

        public void SetTarget(float ratio) { target = Mathf.Clamp01(ratio); }

        public void SetLabel(string text) { label.text = text; }

        public void Snap(float ratio)
        {
            shown = target = Mathf.Clamp01(ratio);
            Apply();
        }

        public void Tick(float dt)
        {
            shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.01f, dt * 2.4f));
            Apply();
        }

        void Apply()
        {
            float w = Mathf.Max(0.001f, innerW * shown);
            fill.sizeDelta = new Vector2(w, innerH);
            glow.sizeDelta = new Vector2(w, innerH + 4f);
            shine.sizeDelta = new Vector2(w, Mathf.Max(2f, innerH * 0.32f));
        }
    }

    public void Build(Transform parent)
    {
        root = UIFactory.Box(parent, "HUD", 0f, 0f, 1600f, 900f);
        UIFactory.Image(root, Color.clear);

        UIFactory.AddPanel(root, "Bars", 24f, 20f, 520f, 118f, new Color(1f, 1f, 1f, 0.87f));

        UIFactory.AddLabel(root, "HPLabel", "生命", 18, new Color(1f, 0.72f, 0.68f),
            40f, 32f, 60f, 28f);
        hpBar = new BarWidget(root, "HPBar", 100f, 30f, 420f, 30f);
        hpBar.Snap(1f);

        UIFactory.AddLabel(root, "StaminaLabel", "体力", 18, new Color(0.68f, 0.86f, 1f),
            40f, 70f, 60f, 28f);
        staminaBar = new BarWidget(root, "StaminaBar", 100f, 68f, 420f, 30f);
        staminaBar.Snap(1f);

        injuryTag = UIFactory.Image(UIFactory.Box(root, "InjuryTag", 40f, 104f, 160f, 26f),
            new Color(0.5f, 0.2f, 0.2f, 0.9f));
        injuryText = UIFactory.AddLabel(root, "InjuryText", "状态：完好", 16, Color.white,
            40f, 104f, 160f, 26f, TextAnchor.MiddleCenter);

        levelText = UIFactory.AddLabel(root, "LevelText", "第 1 关", 24,
            new Color(0.96f, 0.86f, 0.62f), 630f, 24f, 340f, 44f, TextAnchor.MiddleCenter);
        goldText = UIFactory.AddLabel(root, "GoldText", "金币 0", 22,
            new Color(1f, 0.84f, 0.36f), 1320f, 24f, 250f, 44f, TextAnchor.MiddleRight);
        devBadge = UIFactory.AddLabel(root, "DevBadge", "DEV MODE", 16,
            new Color(1f, 0.62f, 0.45f), 1160f, 28f, 160f, 36f,
            TextAnchor.MiddleRight, FontStyle.Bold);
        UIFactory.AddButton(root, "SettingsButton", "设置", 1450f, 76f, 100f, 40f,
            () => { if (OnSettings != null) OnSettings(); },
            new Color(0.2f, 0.22f, 0.24f, 0.95f));

        // Boss 顶部血条：场上有存活 Boss 时才显示
        bossRoot = UIFactory.Box(root, "BossBar", 450f, 76f, 700f, 58f);
        UIFactory.Image(bossRoot, new Color(0.06f, 0.05f, 0.05f, 0.82f));
        bossName = UIFactory.AddLabel(bossRoot, "BossName", "", 19,
            new Color(1f, 0.85f, 0.55f), 14f, 3f, 420f, 24f,
            TextAnchor.MiddleLeft, FontStyle.Bold);
        bossHpText = UIFactory.AddLabel(bossRoot, "BossHp", "", 16, Color.white,
            460f, 3f, 226f, 24f, TextAnchor.MiddleRight);
        bossBar = new BarWidget(bossRoot, "BossFillBar", 14f, 31f, 672f, 18f);
        bossBar.Snap(1f);
        bossRoot.gameObject.SetActive(false);

        UIFactory.AddLabel(root, "Hints", "A/D 移动  空格 跳  J 攻击  K 闪避  L 技能  E 传送  B 商店  I 背包  Esc 返回",
            17, new Color(0.9f, 0.88f, 0.8f), 24f, 840f, 1200f, 40f);

        // 清场后走到出口时显示，明确提示需要按 E 才会传送（不会踩上去自动过关）
        exitPromptRoot = UIFactory.AddPanel(root, "ExitPrompt", 500f, 688f, 600f, 56f,
            new Color(0.1f, 0.08f, 0.06f, 0.86f));
        exitPrompt = UIFactory.AddLabel(exitPromptRoot, "Text", "[E] 进入传送门 · 完成关卡", 22,
            new Color(1f, 0.9f, 0.6f), 0f, 0f, 600f, 56f, TextAnchor.MiddleCenter, FontStyle.Bold);
        exitPromptRoot.gameObject.SetActive(false);

        messageText = UIFactory.AddLabel(root, "Message", "", 22,
            new Color(1f, 0.92f, 0.68f), 300f, 760f, 1000f, 44f, TextAnchor.MiddleCenter,
            FontStyle.Bold);

        EventBus.Message += OnMessage;
        EventBus.GoldChanged += OnGold;
        root.gameObject.SetActive(false); // 默认隐藏，进入关卡时由 Show(true) 打开
    }

    void OnDestroy()
    {
        EventBus.Message -= OnMessage;
        EventBus.GoldChanged -= OnGold;
    }

    void OnGold(int gold)
    {
        RefreshGold(gold);
    }

    /// <summary>开发者模式下金币显示 ∞，并显示 DEV MODE 徽章。</summary>
    public void RefreshGold(int gold)
    {
        if (goldText == null) return;
        goldText.text = "金币 " + DevMode.GoldLabel(gold);
        if (devBadge != null) devBadge.gameObject.SetActive(DevMode.Enabled);
    }

    void OnMessage(string msg)
    {
        messageText.text = msg;
        messageUntil = Time.unscaledTime + 2.2f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (hpBar != null) hpBar.Tick(dt);
        if (staminaBar != null) staminaBar.Tick(dt);
        if (bossBar != null) bossBar.Tick(dt);

        if (Time.unscaledTime > messageUntil && messageText.text.Length > 0)
            messageText.text = "";
        if (exitPromptRoot != null && exitPromptRoot.gameObject.activeSelf && exitPrompt != null)
        {
            Color c = exitPrompt.color;
            c.a = 0.68f + 0.32f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            exitPrompt.color = c;
        }
        UpdateBossBar();
    }

    /// <summary>扫描场上存活 Boss，同步顶部血条；没有 Boss 时整块隐藏。</summary>
    void UpdateBossBar()
    {
        if (bossRoot == null) return;
        EnemyController boss = null;
        for (int i = 0; i < EnemyController.All.Count; i++)
        {
            EnemyController e = EnemyController.All[i];
            if (e == null || e.Dead || e.Data == null || e.Health == null) continue;
            if (e.Data.kind == "boss") { boss = e; break; }
        }
        if (boss == null)
        {
            if (bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(false);
            return;
        }
        if (!bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(true);
        float r = Mathf.Clamp01(boss.Health.Ratio);
        Color c = r > 0.4f ? new Color(0.82f, 0.16f, 0.12f)
                           : new Color(1f, 0.28f, 0.16f);
        bossBar.SetColors(c, c);
        bossBar.SetTarget(r);
        bossName.text = boss.Data.name +
            (boss.Data.id == "boss_final" ? "  ·  最终首领" : "  ·  关卡首领");
        bossHpText.text = Mathf.CeilToInt(boss.Health.CurrentHp) + " / " +
                          Mathf.CeilToInt(boss.Health.MaxHp);
    }

    public void Show(bool visible)
    {
        // UI root 挂在 Canvas 下，必须操作 root 而非脚本所在 GameObject
        if (root != null) root.gameObject.SetActive(visible);
        if (!visible) ShowExitPrompt(false);
    }

    /// <summary>玩家站在清场出口范围内时显示 E 键提示，离开即隐藏。</summary>
    public void ShowExitPrompt(bool visible)
    {
        if (exitPromptRoot == null) return;
        if (exitPromptRoot.gameObject.activeSelf == visible) return;
        exitPromptRoot.gameObject.SetActive(visible);
    }

    public void Bind(HealthComponent health, StaminaComponent stamina, int level, int levelCount)
    {
        if (health == null || stamina == null) return;
        float hpRatio = Mathf.Clamp01(health.Ratio);
        float stRatio = Mathf.Clamp01(stamina.Ratio);

        Color hpColor = hpRatio > 0.55f ? new Color(0.8f, 0.18f, 0.14f)
                      : hpRatio > 0.25f ? new Color(0.95f, 0.46f, 0.12f)
                                        : new Color(1f, 0.24f, 0.16f);
        hpBar.SetColors(hpColor, hpColor);
        hpBar.SetTarget(hpRatio);
        hpBar.SetLabel(Mathf.CeilToInt(health.CurrentHp) + " / " + Mathf.CeilToInt(health.MaxHp));

        Color stColor = stRatio > 0.3f ? new Color(0.16f, 0.52f, 0.88f)
                                       : new Color(0.42f, 0.78f, 1f);
        staminaBar.SetColors(stColor, stColor);
        staminaBar.SetTarget(stRatio);
        staminaBar.SetLabel(Mathf.CeilToInt(stamina.Current) + " / " + Mathf.CeilToInt(stamina.Max));

        levelText.text = "第 " + level + " / " + levelCount + " 关";

        float r = health.Ratio;
        string state = r > 0.7f ? "状态：完好" : r > 0.4f ? "状态：负伤" : "状态：濒危";
        injuryText.text = state + "  体力 ×" + (stamina.Max / Mathf.Max(1f, stamina.BaseMax)).ToString("0.00");
        injuryTag.color = r > 0.7f ? new Color(0.2f, 0.4f, 0.25f, 0.9f)
                         : r > 0.4f ? new Color(0.55f, 0.4f, 0.12f, 0.95f)
                                    : new Color(0.65f, 0.14f, 0.14f, 0.95f);
    }
}
