using UnityEngine;
using UnityEditor;

/// <summary>验证所有敌人 id 都能映射到可加载的素材，避免运行退化成纯色块。</summary>
public static class EnemySpriteCheck
{
    public static void Run()
    {
        string[] ids = { "grunt", "charger", "ranged", "bomber", "shield", "elite", "boss_mini", "boss_final" };
        int ok = 0;
        string report = "";
        foreach (string id in ids)
        {
            // 通过反射调用 EnemyController 的私有映射，保持与运行时一致
            var mi = typeof(EnemyController).GetMethod("SpriteNameFor",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            string name = (string)mi.Invoke(null, new object[] { id });
            Texture2D tex = Resources.Load<Texture2D>("Art/Enemies/" + name);
            bool loaded = tex != null;
            if (loaded) ok++;
            report += id + " -> " + name + " : " + (loaded ? ("OK " + tex.width + "x" + tex.height) : "缺失!") + "\n";
        }
        string result = "敌人素材映射 " + ok + "/" + ids.Length + "\n" + report;
        Debug.Log("[EnemySpriteCheck]\n" + result);
        System.IO.File.WriteAllText("F:/2Dgame Demo/unity/enemy_sprite_result.txt", result);
    }
}
