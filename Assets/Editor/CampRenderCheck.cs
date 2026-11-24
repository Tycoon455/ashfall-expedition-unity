using UnityEngine;
using UnityEditor;

/// <summary>离屏渲染营地四个站点，导出截图确认模型显示与比例。</summary>
public static class CampRenderCheck
{
    public static void Run()
    {
        GameObject host = new GameObject("CampRenderHost");
        CampMap map = host.AddComponent<CampMap>();
        map.Build(host.transform, null);
        Transform world = host.transform.Find("CampWorld");
        if (world == null) { Debug.LogError("[CampRenderCheck] 无 CampWorld"); return; }
        world.gameObject.SetActive(true);

        GameObject camGo = new GameObject("ShotCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.09f, 0.08f, 1f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        const int w = 640, h = 420;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;

        string dir = "F:/2Dgame Demo/unity/camp_shots/";
        System.IO.Directory.CreateDirectory(dir);
        string report = "";
        foreach (CampMap.Station st in CampMap.Stations)
        {
            camGo.transform.position = new Vector3(st.x, GameConfig.GroundY + 2f, -10f);
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(dir + "station_" + st.id + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            RenderTexture.active = null;
            report += st.id + " -> station_" + st.id + ".png\n";
        }

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(host);
        Debug.Log("[CampRenderCheck]\n" + report);
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/camp_render_result.txt", report);
    }
}
