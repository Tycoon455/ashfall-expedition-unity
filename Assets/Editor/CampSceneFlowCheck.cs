using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>验证进入营地时清掉关卡场景（否则废土背景跟随镜头盖住营地背景、出现橙色块）。</summary>
public static class CampSceneFlowCheck
{
    public static void Run()
    {
        string report = "";
        // 1. 营地背景素材存在
        Texture2D campBg = Resources.Load<Texture2D>("Art/Backgrounds/camp");
        report += "营地背景 camp.png: " + (campBg != null ? "OK " + campBg.width + "x" + campBg.height : "缺失!") + "\n";

        // 2. CompleteLevel 进入营地前调用了 levels.Clear()
        string src = File.ReadAllText("Assets/Scripts/GameController.cs");
        int start = src.IndexOf("void CompleteLevel()");
        int end = start >= 0 ? src.IndexOf("\n    void ", start + 10) : -1;
        string body = (start >= 0 && end > start) ? src.Substring(start, end - start) : "";
        bool clear = body.Contains("levels.Clear()");
        report += "CompleteLevel 清理关卡场景 levels.Clear(): " + (clear ? "OK" : "缺失!") + "\n";

        // 3. LevelManager.Clear 会清空 backdrop 引用（停止跟随镜头）
        string lsrc = File.ReadAllText("Assets/Scripts/Gameplay/LevelManager.cs");
        int cstart = lsrc.IndexOf("public void Clear()");
        string cbody = cstart >= 0 ? lsrc.Substring(cstart) : "";
        bool nullBackdrop = cbody.Contains("backdrop = null");
        bool destroyBuilt = cbody.Contains("Destroy(go)");
        report += "Clear 清除背景引用: " + (nullBackdrop ? "OK" : "缺失!") +
                  " / 销毁场景对象: " + (destroyBuilt ? "OK" : "缺失!") + "\n";

        bool pass = campBg != null && clear && nullBackdrop && destroyBuilt;
        Debug.Log("[CampSceneFlowCheck] " + (pass ? "PASS" : "FAIL") + "\n" + report);
        File.WriteAllText("F:/2Dgame Demo/unity/camp_flow_result.txt", report);
    }
}
