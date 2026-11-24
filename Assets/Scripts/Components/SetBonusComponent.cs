using System.Collections.Generic;
using UnityEngine;

/// <summary>四件同套才激活；混搭不触发。倍率型与加成型分开取值。</summary>
public class SetBonusComponent : MonoBehaviour
{
    public SetData ActiveSet { get; private set; }
    public string ActiveSetId { get { return ActiveSet == null ? "" : ActiveSet.id; } }

    readonly Dictionary<string, float> values = new Dictionary<string, float>();

    public void Refresh(InventoryComponent inv)
    {
        values.Clear();
        ActiveSet = null;
        if (inv == null) return;

        string candidate = "";
        bool same = true;
        for (int i = 0; i < InventoryComponent.Slots.Length; i++)
        {
            ItemData item = inv.EquippedItem(InventoryComponent.Slots[i]);
            string setId = item == null ? "" : item.set_id;
            if (string.IsNullOrEmpty(setId)) { same = false; break; }
            if (i == 0) candidate = setId;
            else if (setId != candidate) { same = false; break; }
        }
        if (!same || string.IsNullOrEmpty(candidate)) return;

        ActiveSet = GameData.Set(candidate);
        if (ActiveSet == null || ActiveSet.effects == null) return;
        foreach (StatEntry e in ActiveSet.effects) values[e.key] = e.value;
    }

    /// <summary>加成型（crit / reflect / low_hp_damage / skill_cdr 等），缺省 0。</summary>
    public float Add(string key)
    {
        float v;
        return values.TryGetValue(key, out v) ? v : 0f;
    }

    /// <summary>倍率型（speed_mult / range / skill_cost 等），缺省 1。</summary>
    public float Mult(string key)
    {
        float v;
        return values.TryGetValue(key, out v) ? v : 1f;
    }

    public bool Has(string key)
    {
        return values.ContainsKey(key);
    }
}
