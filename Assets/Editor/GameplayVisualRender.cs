using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>离屏渲染：平铺地面 + 小怪头顶血条（满血/残血/精英）的实际画面。</summary>
public static class GameplayVisualRender
{
    public static void Run()
    {
        GameData.Load();
        float floor = GameConfig.GroundY;

        GameObject ground = new GameObject("Ground");
        SpriteRenderer gr = ground.AddComponent<SpriteRenderer>();
        gr.sprite = GroundArt.Ground();
        gr.drawMode = SpriteDrawMode.Tiled;
        gr.size = new Vector2(24f, 1.2f);
        gr.sortingOrder = -5;
        ground.transform.position = new Vector3(0f, floor - 1.2f, 0f);

        float[] xs = { -4.2f, -0.6f, 3.4f };
        string[] ids = { "grunt", "ranged", "elite" };
        GameObject[] enemies = new GameObject[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            GameObject go = new GameObject("Enemy_" + ids[i]);
            EnemyController e = go.AddComponent<EnemyController>();
            e.Setup(GameData.Enemy(ids[i]), null, xs[i], floor);
            if (i == 1) e.ApplyDamage(22f, null);   // 第二只打到残血，验证血条变色与缩短
            enemies[i] = go;
        }

        GameObject camGo = new GameObject("ShotCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.1f, 0.09f, 1f);
        camGo.transform.position = new Vector3(0f, -0.45f, -10f);

        const int w = 900, h = 500;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        string path = "F:/2Dgame Demo/unity/gameplay_visual_shot.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.active = null;

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        foreach (GameObject go in enemies) Object.DestroyImmediate(go);
        Object.DestroyImmediate(ground);
        Debug.Log("[GameplayVisualRender] 截图 -> " + path);
    }
}
