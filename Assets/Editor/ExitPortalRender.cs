using UnityEngine;
using UnityEditor;

/// <summary>离屏渲染关卡出口传送门精灵，确认模型显示与比例。</summary>
public static class ExitPortalRender
{
    public static void Run()
    {
        // 直接实例化传送门精灵（与 LevelManager.Build 里的加载/缩放逻辑一致）
        GameObject portal = new GameObject("Exit");
        SpriteRenderer sr = portal.AddComponent<SpriteRenderer>();
        Sprite sp = ResourceFactory.Sprite("Props/exit_portal", 100f);
        sr.sprite = sp;
        sr.sortingOrder = -2;
        const float exitHeight = 3.6f;
        float es = sp != null ? exitHeight / Mathf.Max(0.01f, sp.bounds.size.y) : 1f;
        portal.transform.localScale = new Vector3(es, es, 1f);
        float floor = GameConfig.GroundY;
        portal.transform.position = new Vector3(0f, floor + exitHeight * 0.5f, 0f);

        GameObject ground = new GameObject("Ground");
        SpriteRenderer gr = ground.AddComponent<SpriteRenderer>();
        gr.sprite = ResourceFactory.SolidSprite(new Color(0.26f, 0.24f, 0.21f));
        gr.sortingOrder = -5;
        ground.transform.localScale = new Vector3(20f, 1.2f, 1f);
        ground.transform.position = new Vector3(0f, floor - 0.6f, 0f);

        GameObject camGo = new GameObject("ShotCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.11f, 0.1f, 1f);
        camGo.transform.position = new Vector3(0f, -0.5f, -10f);

        const int w = 640, h = 420;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        string path = "F:/2Dgame Demo/unity/exit_portal_shot.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.active = null;

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(portal);
        Object.DestroyImmediate(ground);
        Debug.Log("[ExitPortalRender] 截图 -> " + path);
    }
}
