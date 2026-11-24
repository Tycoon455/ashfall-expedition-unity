using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;
    public string profession = "warrior";
    public int level = 1;
    public int playerLevel = 1;
    public int exp = 0;
    public int gold = 0;
    public int maxHpBonus = 0;
    public List<string> bag = new List<string>();
    public List<string> equipped = new List<string>() { "", "", "", "" };
    public int shopRefresh = 0;
    public string savedAt = "";
}

public static class SaveManager
{
    static string Dir
    {
        get
        {
            string d = Path.Combine(Application.persistentDataPath, "saves");
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            return d;
        }
    }

    public static string SlotPath(int slot)
    {
        return Path.Combine(Dir, "slot" + slot + ".json");
    }

    public static bool Has(int slot)
    {
        return File.Exists(SlotPath(slot));
    }

    public static SaveData Load(int slot)
    {
        try
        {
            string json = File.ReadAllText(SlotPath(slot));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || data.version != 1) return null;
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveManager] 读取失败 slot" + slot + ": " + e.Message);
            return null;
        }
    }

    public static void Save(int slot, SaveData data)
    {
        try
        {
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
            EventBus.RaiseMessage("已存档到槽位 " + slot);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveManager] 保存失败: " + e.Message);
        }
    }

    public static void Delete(int slot)
    {
        try
        {
            if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot));
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    public static string Describe(int slot)
    {
        SaveData d = Load(slot);
        if (d == null) return "空槽位";
        ProfessionData p = GameData.Profession(d.profession);
        string name = p == null ? d.profession : p.name;
        return name + " · 第 " + d.level + " 关 · " + d.gold + " 金 · " + d.savedAt;
    }
}
