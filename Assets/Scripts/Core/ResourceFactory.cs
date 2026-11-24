using System.Collections.Generic;
using UnityEngine;

/// <summary>运行时把 Resources 里的 PNG 转成 Sprite，绕开纹理导入设置。</summary>
public static class ResourceFactory
{
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static Font uiFont;
    static Sprite whiteSolid;
    static Sprite glowSprite;

    /// <summary>径向渐变白色光球，用于掉落物光晕/特效，避免纯色方块边缘生硬。</summary>
    public static Sprite GlowSprite
    {
        get
        {
            if (glowSprite != null) return glowSprite;
            const int S = 64;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f;
                float dy = (y + 0.5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a); // smoothstep 柔化边缘
                px[y * S + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            glowSprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, S, S),
                new Vector2(0.5f, 0.5f), 100f);
            return glowSprite;
        }
    }

    /// <summary>缓存的纯白 1x1 精灵，投射物/特效通过 Image.color 上色复用，避免频繁建纹理。</summary>
    public static Sprite WhiteSolid
    {
        get
        {
            if (whiteSolid == null)
                whiteSolid = UnityEngine.Sprite.Create(Solid(Color.white),
                    new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return whiteSolid;
        }
    }

    public static Sprite Sprite(string path, float pixelsPerUnit = 100f)
    {
        string key = path + "@" + pixelsPerUnit;
        Sprite cached;
        if (sprites.TryGetValue(key, out cached)) return cached;

        Texture2D tex = Resources.Load<Texture2D>("Art/" + path);
        if (tex == null)
        {
            Debug.LogWarning("[ResourceFactory] 缺少素材 Art/" + path);
            return null;
        }
        tex.filterMode = FilterMode.Bilinear;
        Sprite s = UnityEngine.Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        sprites[key] = s;
        return s;
    }

    /// <summary>Windows 下用系统中文字体，避免内置字体显示豆腐块。</summary>
    public static Font Font()
    {
        if (uiFont != null) return uiFont;
        uiFont = UnityEngine.Font.CreateDynamicFontFromOSFont(
            new string[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 16);
        if (uiFont == null)
            uiFont = Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
        return uiFont;
    }

    public static Texture2D Solid(Color color)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    public static Sprite SolidSprite(Color color)
    {
        return UnityEngine.Sprite.Create(Solid(color), new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), 1f);
    }
}
