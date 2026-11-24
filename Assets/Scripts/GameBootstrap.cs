using UnityEngine;

/// <summary>空场景按 Play 即自动启动，不需要手工拖组件或配置场景。</summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // 启动即套用上次保存的显示设置（设置面板里修改后也会立即生效）
        if (!Application.isBatchMode) DisplaySettings.ApplyAll();

        if (Object.FindObjectOfType<GameController>() != null) return;
        GameObject go = new GameObject("GameRoot");
        go.AddComponent<GameController>();
        Debug.Log("[GameBootstrap] 灰烬远征已启动");
    }
}
