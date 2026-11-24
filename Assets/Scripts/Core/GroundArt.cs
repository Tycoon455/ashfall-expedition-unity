using UnityEngine;

/// <summary>
/// 程序化生成的像素风地面纹理：不规则碎石顶沿 + 泥土纵向渐变 + 地层带 + 裂纹 + 石块 + 草簇。
/// 128x48 @40PPU = 3.2x1.2 世界单位，配合 SpriteDrawMode.Tiled 平铺，不会因关卡宽度拉伸变形。
/// </summary>
public static class GroundArt
{
    const int W = 128;
    const int H = 48;
    const float PPU = 40f;
    static Sprite cached;

    public static Sprite Ground()
    {
        if (cached != null) return cached;

        Color32[] px = new Color32[W * H];
        System.Random rng = new System.Random(20261123);

        // 泥土底色：越往下越暗，叠加细噪点和横向地层带
        Color top = new Color(0.22f, 0.20f, 0.17f);
        Color bottom = new Color(0.075f, 0.07f, 0.065f);
        for (int y = 0; y < H; y++)
        {
            float t = y / (float)(H - 1);
            Color baseC = Color.Lerp(top, bottom, Mathf.Pow(t, 0.78f));
            float bandTint = (Hash(y / 9, 7) - 0.5f) * 0.03f;
            for (int x = 0; x < W; x++)
            {
                float n = (Hash(x, y) - 0.5f) * 0.045f;
                Set(px, x, y, baseC + new Color(n + bandTint, n * 0.9f + bandTint,
                    n * 0.75f + bandTint));
            }
        }

        // 裂纹：从上半部向下延伸的折线，颜色接近黑
        for (int i = 0; i < 7; i++)
        {
            int x = rng.Next(W);
            int y = 3 + rng.Next(9);
            int len = 6 + rng.Next(14);
            int dir = rng.Next(2) == 0 ? 1 : -1;
            for (int k = 0; k < len && y < H - 1; k++)
            {
                Set(px, x, y, new Color(0.045f, 0.04f, 0.035f));
                // 每 5~7 像素才轻微折一次，避免形成规律的 V 形
                if (k % (5 + i % 3) == 0) x = (x + dir + W) % W;
                y++;
            }
        }

        // 散落石块：带一点上部高光、下部阴影
        for (int i = 0; i < 12; i++)
        {
            int cx = rng.Next(W);
            int cy = 10 + rng.Next(H - 16);
            int r = 1 + rng.Next(2);
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > r * r + 1) continue;
                int xx = (cx + dx + W) % W;
                int yy = Mathf.Clamp(cy + dy, 0, H - 1);
                float shade = 0.20f + Hash(xx, yy) * 0.08f - Mathf.Max(0, dy) * 0.035f;
                Set(px, xx, yy, new Color(shade, shade * 0.96f, shade * 0.88f));
            }
        }

        // 顶部碎石沿：每列高度随机 2~5 像素，稀疏点缀草簇
        for (int x = 0; x < W; x++)
        {
            int lip = 2 + Mathf.FloorToInt(Hash(x, 91) * 4f);
            for (int y = 0; y < lip; y++)
            {
                float v = 0.22f + Hash(x, y) * 0.10f - y * 0.015f;
                Set(px, x, y, new Color(v * 0.95f, v * 0.92f, v * 0.82f));
            }
            if (Hash(x, 55) > 0.88f)
            {
                Set(px, x, lip - 1, new Color(0.13f, 0.22f, 0.11f));
                if (Hash(x, 56) > 0.5f && lip >= 2)
                    Set(px, x, lip - 2, new Color(0.16f, 0.26f, 0.13f));
            }
        }

        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.name = "ProceduralGroundTex";
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixels32(px);
        tex.Apply(false, false);

        cached = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f),
            PPU, 0, SpriteMeshType.FullRect);
        cached.name = "ProceduralGround";
        return cached;
    }

    static void Set(Color32[] px, int x, int y, Color c)
    {
        // SetPixels32 的第 0 行是图片底部；这里逻辑 y=0 表示地面顶沿，故翻转
        px[(H - 1 - y) * W + x] = c;
    }

    /// <summary>确定性哈希，保证每次运行地面完全一致，且左右边缘可无缝平铺。</summary>
    static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }
    }
}
