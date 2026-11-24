using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// 一键构建 Windows 独立版（双击 exe 即玩，无需编辑器）。
/// 用法：Tuanjie.exe -batchmode -quit -projectPath ... -executeMethod BuildWindows.Run -logFile ...
/// 产物：F:/2Dgame Demo/灰烬远征/灰烬远征.exe
/// </summary>
public static class BuildWindows
{
    public static void Run()
    {
        // 输出目录放在工程外层的「灰烬远征」文件夹，方便用户找到
        string projectRoot = Directory.GetCurrentDirectory();          // .../unity
        string demoRoot = Directory.GetParent(projectRoot).FullName;   // F:/2Dgame Demo
        string outDir = Path.Combine(demoRoot, "灰烬远征");
        string exeName = "灰烬远征.exe";
        string exePath = Path.Combine(outDir, exeName);

        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        // 公司/产品名会写入 exe 属性与默认存档路径
        PlayerSettings.companyName = "Ashfall Studio";
        PlayerSettings.productName = "灰烬远征";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        // 构建默认无边框全屏；玩家在设置里改过的话，启动时 DisplaySettings 会覆盖为上次选择
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;

        BuildPlayerOptions opts = new BuildPlayerOptions();
        opts.scenes = new[] { "Assets/Scenes/Game.unity" };
        opts.locationPathName = exePath;
        opts.target = BuildTarget.StandaloneWindows64;
        opts.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary summary = report.summary;

        string result =
            "BUILD result=" + summary.result +
            " size=" + (summary.totalSize / 1048576f).ToString("F1") + "MB" +
            " errors=" + summary.totalErrors +
            " warnings=" + summary.totalWarnings +
            " time=" + summary.totalTime.TotalSeconds.ToString("F1") + "s" +
            "\nEXE=" + exePath;
        File.WriteAllText(Path.Combine(projectRoot, "build_result.txt"), result);
        Debug.Log("[BuildWindows] " + result);

        if (summary.result != BuildResult.Succeeded)
            Debug.LogError("[BuildWindows] 构建失败，请查看 build log");
    }
}
