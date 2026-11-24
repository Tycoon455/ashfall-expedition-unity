using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>离屏渲染地面掉落物，确认宝珠颜色与名称浮字可见。</summary>
public static class LootDropRender
{
    public static void Run()
    {
        GameObject ground = new GameObject("Ground");
        SpriteRenderer gr = ground.AddComponent<SpriteRenderer>();
        gr.sprite = GroundArt.Ground();
        gr.drawMode = SpriteDrawMode.Tiled;
        gr.size = new Vector2(20f, 1.2f);
        gr.sortingOrder = -5;
        ground.transform.position = new Vector3(0f, GameConfig.GroundY - 1.2f, 0f);

        LootDrop.Spawn(new Vector3(-3f, 0f, 0f), LootDrop.Kind.Gold, "", 0, "+8 金");
        LootDrop.Spawn(new Vector3(0f, 0f, 0f), LootDrop.Kind.Item, "", 2, "锈蚀宽刃 [蓝]");
        LootDrop.Spawn(new Vector3(3.5f, 0f, 0f), LootDrop.Kind.Item, "", 3, "石甲战铠 [紫]");

        GameObject camGo = new GameObject("ShotCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 3.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.1f, 0.09f, 1f);
        camGo.transform.position = new Vector3(0f, -1.1f, -10f);

        const int w = 900, h = 420;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        string path = "F:/2Dgame Demo/unity/loot_drop_shot.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.active = null;

        LootDrop.ClearAll();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(ground);
        Debug.Log("[LootDropRender] 截图 -> " + path);
    }
}
