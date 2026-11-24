using System.Collections.Generic;
using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    public static readonly string[] Slots = { "weapon", "helmet", "armor", "amulet" };

    public int Gold = 0;
    public readonly List<string> Bag = new List<string>();
    public readonly string[] Equipped = { "", "", "", "" };

    public event System.Action Changed;

    public bool Add(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || GameData.Item(itemId) == null) return false;
        if (Bag.Count >= GameConfig.InventoryCapacity)
        {
            EventBus.RaiseMessage("背包已满");
            return false;
        }
        Bag.Add(itemId);
        Raise();
        return true;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Bag.Count) return;
        Bag.RemoveAt(index);
        Raise();
    }

    public bool Buy(ItemData item)
    {
        if (item == null) return false;
        // 开发者模式：购买免费，不修改实际金币数值（关闭后立即恢复正常经济）
        bool free = DevMode.FreePurchase;
        if (!free && Gold < item.price)
        {
            EventBus.RaiseMessage("金币不足");
            return false;
        }
        if (!free) Gold -= item.price;
        Bag.Add(item.id);
        EventBus.RaiseGoldChanged(Gold);
        EventBus.RaiseMessage("购买：" + item.name);
        Raise();
        return true;
    }

    public void SellAt(int index)
    {
        if (index < 0 || index >= Bag.Count) return;
        ItemData item = GameData.Item(Bag[index]);
        if (item == null) return;
        Gold += Mathf.Max(1, item.price / 2);
        Bag.RemoveAt(index);
        EventBus.RaiseGoldChanged(Gold);
        EventBus.RaiseMessage("卖出：" + item.name);
        Raise();
    }

    public void EquipAt(int bagIndex, int slot)
    {
        if (bagIndex < 0 || bagIndex >= Bag.Count || slot < 0 || slot >= Slots.Length) return;
        ItemData item = GameData.Item(Bag[bagIndex]);
        if (item == null || item.type != Slots[slot])
        {
            EventBus.RaiseMessage("装备类型不匹配");
            return;
        }
        if (!string.IsNullOrEmpty(Equipped[slot])) Bag.Add(Equipped[slot]);
        Equipped[slot] = item.id;
        Bag.RemoveAt(bagIndex);
        Raise();
    }

    public void Unequip(int slot)
    {
        if (slot < 0 || slot >= Slots.Length) return;
        if (string.IsNullOrEmpty(Equipped[slot])) return;
        if (Bag.Count >= GameConfig.InventoryCapacity) return;
        Bag.Add(Equipped[slot]);
        Equipped[slot] = "";
        Raise();
    }

    public ItemData EquippedItem(string slot)
    {
        int i = System.Array.IndexOf(Slots, slot);
        if (i < 0 || string.IsNullOrEmpty(Equipped[i])) return null;
        return GameData.Item(Equipped[i]);
    }

    public int Count(string itemId)
    {
        int n = 0;
        foreach (string s in Bag) if (s == itemId) n++;
        return n;
    }

    public bool UseConsumable(int bagIndex, HealthComponent health)
    {
        if (bagIndex < 0 || bagIndex >= Bag.Count) return false;
        ItemData item = GameData.Item(Bag[bagIndex]);
        if (item == null || item.type != "consumable") return false;
        if (health != null && health.CurrentHp >= health.MaxHp)
        {
            EventBus.RaiseMessage("生命已满");
            return false;
        }
        health.Heal(item.heal);
        RemoveAt(bagIndex);
        EventBus.RaiseMessage("使用 " + item.name + "，回复 " + item.heal + " HP");
        return true;
    }

    public int BonusHp()
    {
        int total = 0;
        foreach (string slot in Slots)
            total += Mathf.RoundToInt(GameData.Stat(EquippedItem(slot), "hp"));
        return total;
    }

    public int BonusStamina()
    {
        int total = 0;
        foreach (string slot in Slots)
            total += Mathf.RoundToInt(GameData.Stat(EquippedItem(slot), "stamina"));
        return total;
    }

    public int BonusDefense()
    {
        int total = 0;
        foreach (string slot in Slots)
            total += Mathf.RoundToInt(GameData.Stat(EquippedItem(slot), "def"));
        return total;
    }

    public void LoadFrom(SaveData save)
    {
        Gold = save.gold;
        Bag.Clear();
        foreach (string id in save.bag) Bag.Add(id);
        for (int i = 0; i < Slots.Length; i++)
            Equipped[i] = i < save.equipped.Count ? save.equipped[i] : "";
        Raise();
    }

    public void WriteTo(SaveData save)
    {
        save.gold = Gold;
        save.bag = new List<string>(Bag);
        save.equipped = new List<string>(Equipped);
    }

    void Raise()
    {
        if (Changed != null) Changed();
    }
}
