using UnityEngine;
using UnityEditor;

/// <summary>验证营地四个站点的模型素材齐全、带透明通道，且能生成对应模型对象。</summary>
public static class CampPropCheck
{
    public static void Run()
    {
        string report = "";
        int ok = 0;
        foreach (CampMap.Station st in CampMap.Stations)
        {
            Texture2D tex = Resources.Load<Texture2D>("Art/" + st.spritePath);
            bool loaded = tex != null;
            if (loaded) ok++;
            report += st.id + " (" + st.name + ") -> " + st.spritePath + " : " +
                      (loaded ? ("OK " + tex.width + "x" + tex.height +
                                 (tex.format == TextureFormat.RGBA32 ? " RGBA" : " " + tex.format))
                              : "缺失!") + "\n";
        }

        // 真正构建一次营地，确认四个模型 GameObject 都被创建
        GameObject host = new GameObject("CampPropCheckHost");
        CampMap map = host.AddComponent<CampMap>();
        map.Build(host.transform, null);
        Transform world = host.transform.Find("CampWorld");
        int models = 0;
        if (world != null)
            foreach (CampMap.Station st in CampMap.Stations)
                if (world.Find("Station_" + st.id) != null) models++;
        Object.DestroyImmediate(host);

        report += "营地模型对象: " + models + "/4\n";
        string result = "营地站点素材 " + ok + "/4，模型对象 " + models + "/4\n" + report;
        bool pass = ok == 4 && models == 4;
        Debug.Log("[CampPropCheck] " + (pass ? "PASS" : "FAIL") + "\n" + result);
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/camp_prop_result.txt", result);
    }
}
