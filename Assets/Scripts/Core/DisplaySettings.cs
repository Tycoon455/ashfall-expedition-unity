using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>显示模式预设：对应设置面板里的三个按钮。</summary>
public enum DisplayPreset
{
    ExclusiveFullscreen = 0, // 独占全屏
    BorderlessFullscreen = 1, // 无边框全屏窗口
    Windowed = 2             // 窗口化
}

/// <summary>一组可选分辨率（只按宽高区分，刷新率沿用当前显示器）。</summary>
[Serializable]
public struct ResolutionOption
{
    public int width;
    public int height;

    public ResolutionOption(int w, int h) { width = w; height = h; }

    public string Label { get { return width + " × " + height; } }
}

/// <summary>
/// 显示设置：窗口模式、分辨率、UI 缩放。
/// 仅改变 Screen / CanvasScaler 的显示参数，不创建、不替换任何角色或场景模型。
/// 设置用 PlayerPrefs 持久化，重启 exe 后自动恢复。
/// </summary>
public static class DisplaySettings
{
    const string ModeKey = "ashfall_display_mode_v1";
    const string WidthKey = "ashfall_display_width_v1";
    const string HeightKey = "ashfall_display_height_v1";
    const string UiScaleKey = "ashfall_ui_scale_v1";

    const float BaseReferenceWidth = 1600f;
    const float BaseReferenceHeight = 900f;

    static readonly float[] UiScaleSteps =
    {
        0.9f, 1f, 1.1f, 1.2f, 1.35f, 1.5f
    };

    static ResolutionOption[] cachedResolutions;

    public static event Action UiScaleChanged;

    /// <summary>首次进入游戏的默认模式：无边框全屏，直接解决窗口偏小的问题。</summary>
    public static DisplayPreset DefaultPreset { get { return DisplayPreset.BorderlessFullscreen; } }

    public static DisplayPreset Preset
    {
        get
        {
            int v = PlayerPrefs.GetInt(ModeKey, (int)DefaultPreset);
            return (DisplayPreset)Mathf.Clamp(v, 0, 2);
        }
        set { PlayerPrefs.SetInt(ModeKey, (int)value); PlayerPrefs.Save(); }
    }

    public static int Width
    {
        get { return Mathf.Max(640, PlayerPrefs.GetInt(WidthKey, DefaultWidth())); }
        set { PlayerPrefs.SetInt(WidthKey, Mathf.Max(640, value)); PlayerPrefs.Save(); }
    }

    public static int Height
    {
        get { return Mathf.Max(360, PlayerPrefs.GetInt(HeightKey, DefaultHeight())); }
        set { PlayerPrefs.SetInt(HeightKey, Mathf.Max(360, value)); PlayerPrefs.Save(); }
    }

    /// <summary>UI 缩放只影响 CanvasScaler 参考分辨率，不改变世界中的精灵/模型尺寸。</summary>
    public static float UiScale
    {
        get { return Mathf.Clamp(PlayerPrefs.GetFloat(UiScaleKey, 1f), 0.8f, 1.6f); }
        set { SetUiScale(value, true); }
    }

    public static void SetUiScale(float scale, bool apply = true)
    {
        scale = Mathf.Clamp(scale, 0.8f, 1.6f);
        PlayerPrefs.SetFloat(UiScaleKey, scale);
        PlayerPrefs.Save();
        if (apply) ApplyUiScale();
    }

    public static int UiScaleIndex
    {
        get
        {
            float s = UiScale;
            int best = 0;
            float bestDiff = Mathf.Abs(UiScaleSteps[0] - s);
            for (int i = 1; i < UiScaleSteps.Length; i++)
            {
                float diff = Mathf.Abs(UiScaleSteps[i] - s);
                if (diff < bestDiff) { best = i; bestDiff = diff; }
            }
            return best;
        }
        set
        {
            int i = Mathf.Clamp(value, 0, UiScaleSteps.Length - 1);
            SetUiScale(UiScaleSteps[i], true);
        }
    }

    public static string UiScaleLabel()
    {
        return Mathf.RoundToInt(UiScale * 100f) + "%";
    }

    public static void StepUiScale(int delta)
    {
        UiScaleIndex = UiScaleIndex + delta;
    }

    public static string ModeLabel(DisplayPreset preset)
    {
        switch (preset)
        {
            case DisplayPreset.ExclusiveFullscreen: return "全屏";
            case DisplayPreset.BorderlessFullscreen: return "无边框";
            default: return "窗口化";
        }
    }

    public static FullScreenMode UnityMode(DisplayPreset preset)
    {
        switch (preset)
        {
            case DisplayPreset.ExclusiveFullscreen: return FullScreenMode.ExclusiveFullScreen;
            case DisplayPreset.BorderlessFullscreen: return FullScreenMode.FullScreenWindow;
            default: return FullScreenMode.Windowed;
        }
    }

    /// <summary>常用分辨率 + 当前桌面分辨率（若不在列表中则追加）。</summary>
    public static ResolutionOption[] Resolutions
    {
        get
        {
            if (cachedResolutions != null) return cachedResolutions;

            var list = new System.Collections.Generic.List<ResolutionOption>();
            AddOnce(list, 1280, 720);
            AddOnce(list, 1366, 768);
            AddOnce(list, 1600, 900);
            AddOnce(list, 1920, 1080);
            AddOnce(list, 2560, 1440);
            AddOnce(list, DefaultWidth(), DefaultHeight());
            AddOnce(list, Width, Height);
            cachedResolutions = list.ToArray();
            return cachedResolutions;
        }
    }

    public static ResolutionOption CurrentResolution()
    {
        ResolutionOption[] all = Resolutions;
        int w = Width, h = Height;
        for (int i = 0; i < all.Length; i++)
            if (all[i].width == w && all[i].height == h) return all[i];
        return new ResolutionOption(w, h);
    }

    public static int ResolutionIndex()
    {
        ResolutionOption[] all = Resolutions;
        int w = Width, h = Height;
        for (int i = 0; i < all.Length; i++)
            if (all[i].width == w && all[i].height == h) return i;
        return 0;
    }

    public static void SetResolutionIndex(int index, bool apply = true)
    {
        ResolutionOption[] all = Resolutions;
        index = Mathf.Clamp(index, 0, all.Length - 1);
        SetResolution(all[index].width, all[index].height, apply);
    }

    public static void StepResolution(int delta)
    {
        SetResolutionIndex(ResolutionIndex() + delta, true);
    }

    public static void SetResolution(int width, int height, bool apply = true)
    {
        PlayerPrefs.SetInt(WidthKey, Mathf.Max(640, width));
        PlayerPrefs.SetInt(HeightKey, Mathf.Max(360, height));
        PlayerPrefs.Save();
        if (apply) ApplyResolution();
    }

    /// <summary>启动时 / 切换设置时调用。批处理自检模式下不触碰真实屏幕。</summary>
    public static void ApplyAll()
    {
        ApplyResolution();
        ApplyUiScale();
    }

    public static void ApplyResolution()
    {
        if (Application.isBatchMode) return;
        FullScreenMode mode = UnityMode(Preset);
        int w = Width, h = Height;
        Screen.fullScreenMode = mode;
        Screen.SetResolution(w, h, mode); // 沿用显示器当前刷新率，避免旧版 int 重载的弃用警告
        Debug.Log("[显示设置] " + ModeLabel(Preset) + " " + w + "×" + h);
    }

    /// <summary>把 UI 缩放写进 CanvasScaler：缩小参考分辨率即可等比放大全部 UGUI。</summary>
    public static void ApplyTo(CanvasScaler scaler)
    {
        if (scaler == null) return;
        float s = Mathf.Clamp(UiScale, 0.8f, 1.6f);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(BaseReferenceWidth / s, BaseReferenceHeight / s);
        scaler.matchWidthOrHeight = 0f;
    }

    public static void ApplyUiScale()
    {
        CanvasScaler[] scalers = Resources.FindObjectsOfTypeAll<CanvasScaler>();
        for (int i = 0; i < scalers.Length; i++) ApplyTo(scalers[i]);
        if (UiScaleChanged != null) UiScaleChanged();
    }

    /// <summary>恢复首次启动的显示配置。</summary>
    public static void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(ModeKey);
        PlayerPrefs.DeleteKey(WidthKey);
        PlayerPrefs.DeleteKey(HeightKey);
        PlayerPrefs.DeleteKey(UiScaleKey);
        PlayerPrefs.Save();
        cachedResolutions = null;
        ApplyAll();
    }

    public static string ReadableSummary()
    {
        return ModeLabel(Preset) + " · " + CurrentResolution().Label + " · UI " + UiScaleLabel();
    }

    static void AddOnce(System.Collections.Generic.List<ResolutionOption> list, int w, int h)
    {
        w = Mathf.Max(640, w);
        h = Mathf.Max(360, h);
        for (int i = 0; i < list.Count; i++)
            if (list[i].width == w && list[i].height == h) return;
        list.Add(new ResolutionOption(w, h));
    }

    static int DefaultWidth()
    {
        int w = Screen.currentResolution.width;
        return w >= 1024 ? w : 1600;
    }

    static int DefaultHeight()
    {
        int h = Screen.currentResolution.height;
        return h >= 576 ? h : 900;
    }
}
