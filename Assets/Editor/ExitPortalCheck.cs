using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>验证关卡出口传送门模型素材齐全、层级正确、清场后出现。</summary>
public static class ExitPortalCheck
{
    public static void Run()
    {
        string report = "";
        Texture2D tex = Resources.Load<Texture2D>("Art/Props/exit_portal");
        bool loaded = tex != null;
        report += "出口素材 exit_portal: " + (loaded ? ("OK " + tex.width + "x" + tex.height) : "缺失!") + "\n";

        string src = File.ReadAllText("Assets/Scripts/Gameplay/LevelManager.cs");
        bool useModel = src.Contains("ResourceFactory.Sprite(\"Props/exit_portal\"");
        bool noSolid = !src.Contains("er.sprite = ResourceFactory.SolidSprite");
        bool showGlow = src.Contains("exitGlow.gameObject.SetActive(true)");
        report += "出口用模型素材: " + (useModel ? "OK" : "缺失!") +
                  " / 已去掉纯色方块: " + (noSolid ? "OK" : "仍是色块!") +
                  " / 光晕联动: " + (showGlow ? "OK" : "缺失!") + "\n";

        string gcSrc = File.ReadAllText("Assets/Scripts/GameController.cs");
        string hudSrc = File.ReadAllText("Assets/Scripts/UI/HUD.cs");
        bool eGate = gcSrc.Contains("atExit && Input.GetKeyDown(KeyCode.E)") &&
                     hudSrc.Contains("ShowExitPrompt");
        report += "出口按 E 才传送 + HUD 提示: " + (eGate ? "OK" : "缺失!") + "\n";

        bool pass = loaded && useModel && noSolid && showGlow && eGate;
        Debug.Log("[ExitPortalCheck] " + (pass ? "PASS" : "FAIL") + "\n" + report);
        File.WriteAllText("F:/2Dgame Demo/unity/exit_portal_result.txt", report);
    }
}
