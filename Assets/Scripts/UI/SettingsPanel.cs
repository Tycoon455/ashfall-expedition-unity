using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板：显示模式（全屏/无边框/窗口化）、分辨率、UI 缩放、开发者模式。
/// 显示设置只调用 Screen / CanvasScaler，不创建或替换任何角色、敌人、场景模型。
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    public System.Action OnClose;

    RectTransform root;
    System.Action defaultOnClose;
    Button[] modeButtons;
    Text resolutionText;
    Text scaleText;
    Text statusText;
    Text hintText;
    Image toggleImage;
    Text toggleLabel;

    readonly Color normalBtn = new Color(0.25f, 0.23f, 0.2f, 1f);
    readonly Color activeBtn = new Color(0.42f, 0.3f, 0.12f, 1f);

    public bool IsOpen { get { return root != null && root.gameObject.activeSelf; } }

    public void Build(Transform parent, System.Action onClose)
    {
        defaultOnClose = onClose;
        OnClose = onClose;
        root = UIFactory.AddPanel(parent, "SettingsPanel", 390f, 30f, 820f, 840f,
            new Color(1f, 1f, 1f, 0.97f));

        UIFactory.AddLabel(root, "Title", "设 置", 36, new Color(0.96f, 0.82f, 0.56f),
            30f, 16f, 760f, 50f, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFactory.AddLabel(root, "Desc", "显示与调试设置保存在本机，重启游戏后依然生效。", 17,
            new Color(0.66f, 0.62f, 0.55f), 30f, 64f, 760f, 28f, TextAnchor.MiddleCenter);

        // ---------- 显示设置 ----------
        UIFactory.AddLabel(root, "DisplayTitle", "显示设置 · 修改后立即生效", 20,
            new Color(0.96f, 0.84f, 0.58f), 30f, 100f, 760f, 30f,
            TextAnchor.MiddleLeft, FontStyle.Bold);

        RectTransform modeRow = UIFactory.Box(root, "ModeRow", 30f, 136f, 760f, 86f);
        UIFactory.Image(modeRow, new Color(0.13f, 0.12f, 0.1f, 0.92f));
        UIFactory.AddLabel(modeRow, "Name", "窗口模式", 21, new Color(0.94f, 0.88f, 0.74f),
            18f, 12f, 140f, 30f, TextAnchor.MiddleLeft, FontStyle.Bold);
        modeButtons = new Button[3];
        string[] modeNames = { "全屏", "无边框", "窗口化" };
        for (int i = 0; i < modeButtons.Length; i++)
        {
            int idx = i;
            modeButtons[i] = UIFactory.AddButton(modeRow, "Mode" + i, modeNames[i],
                168f + i * 184f, 18f, 176f, 50f,
                () =>
                {
                    DisplaySettings.Preset = (DisplayPreset)idx;
                    DisplaySettings.ApplyAll();
                    Refresh();
                    EventBus.RaiseMessage("显示模式：" + DisplaySettings.ModeLabel(DisplaySettings.Preset));
                }, normalBtn);
        }

        RectTransform resRow = UIFactory.Box(root, "ResolutionRow", 30f, 230f, 760f, 74f);
        UIFactory.Image(resRow, new Color(0.13f, 0.12f, 0.1f, 0.92f));
        UIFactory.AddLabel(resRow, "Name", "分辨率", 21, new Color(0.94f, 0.88f, 0.74f),
            18f, 10f, 140f, 28f, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIFactory.AddButton(resRow, "Prev", "<", 430f, 12f, 44f, 48f,
            () => { DisplaySettings.StepResolution(-1); Refresh(); }, normalBtn);
        resolutionText = UIFactory.AddLabel(resRow, "Value", "", 20, Color.white,
            484f, 12f, 190f, 48f, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFactory.AddButton(resRow, "Next", ">", 684f, 12f, 44f, 48f,
            () => { DisplaySettings.StepResolution(1); Refresh(); }, normalBtn);

        RectTransform scaleRow = UIFactory.Box(root, "ScaleRow", 30f, 312f, 760f, 74f);
        UIFactory.Image(scaleRow, new Color(0.13f, 0.12f, 0.1f, 0.92f));
        UIFactory.AddLabel(scaleRow, "Name", "界面大小", 21, new Color(0.94f, 0.88f, 0.74f),
            18f, 10f, 140f, 28f, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIFactory.AddButton(scaleRow, "Minus", "-", 430f, 12f, 44f, 48f,
            () => { DisplaySettings.StepUiScale(-1); Refresh(); }, normalBtn);
        scaleText = UIFactory.AddLabel(scaleRow, "Value", "", 20, Color.white,
            484f, 12f, 190f, 48f, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFactory.AddButton(scaleRow, "Plus", "+", 684f, 12f, 44f, 48f,
            () => { DisplaySettings.StepUiScale(1); Refresh(); }, normalBtn);

        UIFactory.AddButton(root, "ResetDisplay", "恢复默认", 30f, 394f, 220f, 46f,
            () => { DisplaySettings.ResetToDefault(); Refresh(); EventBus.RaiseMessage("显示设置已恢复默认"); },
            new Color(0.26f, 0.24f, 0.2f, 1f));
        UIFactory.AddButton(root, "ReapplyDisplay", "重新应用", 270f, 394f, 200f, 46f,
            () => { DisplaySettings.ApplyAll(); EventBus.RaiseMessage("显示设置已应用"); },
            new Color(0.26f, 0.24f, 0.2f, 1f));
        UIFactory.AddLabel(root, "ScaleHint", "界面大小只缩放 UGUI 文字与按钮，不改变角色 / 模型比例。",
            14, new Color(0.68f, 0.64f, 0.57f), 500f, 398f, 290f, 38f, TextAnchor.MiddleLeft);
        UIFactory.AddLabel(root, "ModeHint",
            "无边框 = 占满屏幕且可快速切出；全屏 = 独占显示；窗口化按上方分辨率显示。",
            14, new Color(0.68f, 0.64f, 0.57f), 30f, 448f, 760f, 26f, TextAnchor.MiddleLeft);

        // ---------- 调试设置 ----------
        UIFactory.AddLabel(root, "DevTitle", "调试设置", 20,
            new Color(0.96f, 0.84f, 0.58f), 30f, 486f, 760f, 28f,
            TextAnchor.MiddleLeft, FontStyle.Bold);

        RectTransform devRow = UIFactory.Box(root, "DevRow", 30f, 520f, 760f, 96f);
        UIFactory.Image(devRow, new Color(0.13f, 0.12f, 0.1f, 0.92f));
        UIFactory.AddLabel(devRow, "Name", "开发者模式", 22, new Color(0.96f, 0.84f, 0.58f),
            18f, 8f, 170f, 30f, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIFactory.AddLabel(devRow, "Desc", "开启后：直达最高关、金币无限、商店常驻最高品质装备。仅供试玩与调试。",
            15, new Color(0.66f, 0.62f, 0.55f), 18f, 42f, 560f, 44f);
        Button toggle = UIFactory.AddButton(devRow, "Toggle", "", 600f, 24f, 140f, 48f,
            Toggle, new Color(0.25f, 0.23f, 0.2f, 1f));
        toggleImage = toggle.GetComponent<Image>();
        toggleLabel = toggle.transform.Find("Text").GetComponent<Text>();

        RectTransform card = UIFactory.Box(root, "StatusCard", 30f, 626f, 760f, 96f);
        UIFactory.Image(card, new Color(0.16f, 0.1f, 0.09f, 0.92f));
        statusText = UIFactory.AddLabel(card, "Status", "", 15,
            new Color(0.94f, 0.8f, 0.68f), 16f, 8f, 728f, 82f);

        hintText = UIFactory.AddLabel(root, "Hint", "", 14,
            new Color(0.68f, 0.64f, 0.57f), 30f, 730f, 760f, 28f, TextAnchor.MiddleCenter);

        UIFactory.AddButton(root, "Back", "返回（Esc）", 280f, 760f, 260f, 54f,
            Close, new Color(0.42f, 0.3f, 0.12f, 1f));

        Refresh();
        root.gameObject.SetActive(false); // 默认隐藏，Open() 时打开
    }

    void Toggle()
    {
        DevMode.Toggle();
        Refresh();
    }

    void Refresh()
    {
        RefreshDisplay();

        bool on = DevMode.Enabled;
        toggleImage.color = on ? new Color(0.64f, 0.23f, 0.18f, 1f) : normalBtn;
        toggleLabel.text = on ? "已开启" : "已关闭";
        statusText.text =
            "<b>当前显示：</b>" + DisplaySettings.ReadableSummary() + "\n" +
            "<b>开发者模式：</b>" + (on ? "已开启 · 第10关 / 无限金币 / 橙装商店" : "已关闭 · 正常进度与经济") + "\n" +
            "显示设置只调整窗口模式 / 分辨率 / UI 缩放，不会新建或替换任何角色与场景模型。";
        hintText.text = on
            ? "开发者模式需开始新游戏或继续游戏后才会从最高关进入。"
            : "所有设置自动保存到本机；游戏中也可点右上角「设置」调整。";
    }

    void RefreshDisplay()
    {
        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] == null) continue;
            modeButtons[i].GetComponent<Image>().color =
                i == (int)DisplaySettings.Preset ? activeBtn : normalBtn;
        }
        if (resolutionText != null) resolutionText.text = DisplaySettings.CurrentResolution().Label;
        if (scaleText != null) scaleText.text = DisplaySettings.UiScaleLabel();
    }

    /// <summary>从主菜单打开时沿用默认返回主菜单；从游戏中打开时传入游戏的关闭回调。</summary>
    public void Open(System.Action onClose = null)
    {
        OnClose = onClose != null ? onClose : defaultOnClose;
        Refresh();
        root.gameObject.SetActive(true);
    }

    public void Close()
    {
        Hide();
        if (OnClose != null) OnClose();
    }

    public void Hide() { if (root != null) root.gameObject.SetActive(false); }
}
