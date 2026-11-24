using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>用 1600x900 参考分辨率，从左上角像素坐标搭建 UGUI。</summary>
public static class UIFactory
{
    /// <summary>UGUI 按钮/输入分发依赖 EventSystem，缺失则所有点击失效。全局仅创建一个。</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    public static Canvas CreateCanvas()
    {
        EnsureEventSystem();
        GameObject go = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0f; // 按宽度缩放，保证各窗口比例下 UI 都不被裁切
        DisplaySettings.ApplyTo(scaler); // 套用玩家在设置面板里调好的 UI 缩放
        return canvas;
    }

    public static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static Image Image(RectTransform rt, Color color, Sprite sprite = null)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (sprite != null) img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    public static Text Label(RectTransform rt, string text, int size, Color color,
        TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
    {
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = ResourceFactory.Font();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.text = text;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Text AddLabel(Transform parent, string name, string text, int size,
        Color color, float x, float y, float w, float h,
        TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
    {
        return Label(Box(parent, name, x, y, w, h), text, size, color, align, style);
    }

    public static Button AddButton(Transform parent, string name, string text,
        float x, float y, float w, float h, System.Action onClick, Color color)
    {
        RectTransform rt = Box(parent, name, x, y, w, h);
        Image img = Image(rt, color);
        img.raycastTarget = true;
        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1f, 0.88f, 0.6f);
        cb.pressedColor = new Color(0.7f, 0.6f, 0.4f);
        btn.colors = cb;
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        AddLabel(rt, "Text", text, 20, Color.white, 0f, 0f, w, h, TextAnchor.MiddleCenter);
        return btn;
    }

    public static RectTransform AddPanel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        RectTransform rt = Box(parent, name, x, y, w, h);
        Image img = Image(rt, color, ResourceFactory.Sprite("UI/panel_large"));
        img.raycastTarget = true;
        return rt;
    }
}
