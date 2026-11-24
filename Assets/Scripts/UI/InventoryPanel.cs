using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MonoBehaviour
{
    enum Tab { Stats, Equip, Bag }

    RectTransform root;
    RectTransform content;
    Tab current;
    InventoryComponent inventory;
    SetBonusComponent bonus;
    HealthComponent health;
    PlayerController player;

    public void Build(Transform parent, System.Action onClose)
    {
        root = UIFactory.AddPanel(parent, "InventoryPanel", 260f, 70f, 1080f, 760f,
            new Color(1f, 1f, 1f, 0.98f));
        UIFactory.AddLabel(root, "Title", "人物状态", 36,
            new Color(0.96f, 0.84f, 0.58f), 36f, 22f, 500f, 60f, TextAnchor.MiddleLeft,
            FontStyle.Bold);
        UIFactory.AddButton(root, "Close", "关闭（Esc）", 880f, 24f, 160f, 54f,
            () => { Hide(); if (onClose != null) onClose(); },
            new Color(0.34f, 0.18f, 0.16f, 1f));

        UIFactory.AddButton(root, "TabStats", "属性", 40f, 95f, 200f, 52f,
            () => Switch(Tab.Stats), new Color(0.26f, 0.28f, 0.3f, 1f));
        UIFactory.AddButton(root, "TabEquip", "装备", 260f, 95f, 200f, 52f,
            () => Switch(Tab.Equip), new Color(0.26f, 0.28f, 0.3f, 1f));
        UIFactory.AddButton(root, "TabBag", "背包", 480f, 95f, 200f, 52f,
            () => Switch(Tab.Bag), new Color(0.26f, 0.28f, 0.3f, 1f));

        content = UIFactory.Box(root, "Content", 40f, 165f, 1000f, 560f);
        root.gameObject.SetActive(false); // 默认隐藏，Open() 时打开
    }

    public void Open(InventoryComponent inv, SetBonusComponent b, HealthComponent hp, PlayerController p)
    {
        inventory = inv;
        bonus = b;
        health = hp;
        player = p;
        root.gameObject.SetActive(true);
        Switch(Tab.Stats);
    }

    public void Hide() { root.gameObject.SetActive(false); }

    void Switch(Tab tab)
    {
        current = tab;
        Rebuild();
    }

    void ClearContent()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    void Rebuild()
    {
        ClearContent();
        if (current == Tab.Stats) BuildStats();
        else if (current == Tab.Equip) BuildEquip();
        else BuildBag();
    }

    void BuildStats()
    {
        ProfessionData prof = GameData.Profession(GameController.Instance == null
            ? "warrior" : GameController.Instance.ProfessionId);
        if (prof == null) return;
        CombatComponent combat = player == null ? null : player.Combat;
        int atk = prof.attack + (combat == null ? 0 : Mathf.RoundToInt(combat.Damage - prof.attack));
        int def = prof.defense + (inventory == null ? 0 : inventory.BonusDefense());

        UIFactory.AddLabel(content, "S1", "职业：" + prof.name, 26, Color.white, 30f, 20f, 500f, 44f);
        UIFactory.AddLabel(content, "S2", "生命上限：" + Mathf.CeilToInt(health == null ? prof.max_hp : health.MaxHp),
            24, new Color(1f, 0.72f, 0.7f), 30f, 70f, 500f, 40f);
        UIFactory.AddLabel(content, "S3", "体力上限：" + Mathf.CeilToInt(prof.max_stamina) +
            "（随伤势浮动）", 24, new Color(0.65f, 0.84f, 1f), 30f, 116f, 500f, 40f);
        UIFactory.AddLabel(content, "S4", "攻击：" + atk + "    防御：" + def, 24, Color.white,
            30f, 162f, 500f, 40f);
        UIFactory.AddLabel(content, "S5", "暴击：" + ((combat == null ? 0 : combat.CritChance) * 100f).ToString("0") + "%",
            24, new Color(0.95f, 0.85f, 0.6f), 30f, 208f, 500f, 40f);
        UIFactory.AddLabel(content, "S6", "金币：" + (inventory == null ? 0 : inventory.Gold),
            24, new Color(1f, 0.84f, 0.36f), 30f, 254f, 500f, 40f);

        UIFactory.AddLabel(content, "SetTitle", "套装效果", 26,
            new Color(0.96f, 0.82f, 0.55f), 30f, 320f, 300f, 44f);
        string setText = bonus != null && bonus.ActiveSet != null
            ? bonus.ActiveSet.name + "（4/4 激活）\n" + bonus.ActiveSet.bonus
            : "未激活：需凑齐同一套装的 4 件装备";
        UIFactory.AddLabel(content, "SetDesc", setText, 20, new Color(0.86f, 0.83f, 0.76f),
            30f, 370f, 900f, 100f);
    }

    void BuildEquip()
    {
        for (int i = 0; i < InventoryComponent.Slots.Length; i++)
        {
            int slot = i;
            ItemData item = inventory.EquippedItem(InventoryComponent.Slots[i]);
            float y = i * 130f;
            RectTransform card = UIFactory.Box(content, "Slot_" + i, 20f, y, 960f, 112f);
            UIFactory.Image(card, new Color(0.12f, 0.13f, 0.14f, 0.9f),
                ResourceFactory.Sprite("UI/frame_gray"));
            string name = item == null ? "空" : item.name;
            Color c = item == null ? Color.gray : GameConfig.RarityColor(item.rarity);
            UIFactory.AddLabel(card, "SlotName", InventoryComponent.Slots[i] + "：" + name,
                22, c, 30f, 12f, 600f, 44f);
            string stat = item == null ? "未装备"
                : "攻击 +" + Mathf.RoundToInt(GameData.Stat(item, "atk")) +
                  "  防御 +" + Mathf.RoundToInt(GameData.Stat(item, "def")) +
                  "  生命 +" + Mathf.RoundToInt(GameData.Stat(item, "hp"));
            UIFactory.AddLabel(card, "Stat", stat, 17, new Color(0.82f, 0.8f, 0.75f),
                30f, 58f, 600f, 38f);
            UIFactory.AddButton(card, "Unequip", "卸下", 800f, 30f, 130f, 52f,
                () => Unequip(slot), new Color(0.3f, 0.24f, 0.18f, 1f));
        }
    }

    void BuildBag()
    {
        int count = Mathf.Min(inventory.Bag.Count, 16);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            ItemData item = GameData.Item(inventory.Bag[i]);
            if (item == null) continue;
            int col = i / 2;
            int row = i % 2;
            float x = col * 490f;
            float y = row * 270f;
            RectTransform card = UIFactory.Box(content, "Bag_" + i, x, y, 470f, 250f);
            UIFactory.Image(card, new Color(0.12f, 0.13f, 0.14f, 0.9f),
                ResourceFactory.Sprite("UI/frame_" + GameConfig.RarityFiles[Mathf.Clamp(item.rarity, 0, 4)]));

            RectTransform iconRt = UIFactory.Box(card, "Icon", 20f, 20f, 120f, 120f);
            UIFactory.Image(iconRt, Color.white,
                ResourceFactory.Sprite("Icons/icon_" + IconKey(item), 180f));
            UIFactory.AddLabel(card, "Name", item.name, 21, GameConfig.RarityColor(item.rarity),
                160f, 20f, 290f, 36f);
            UIFactory.AddLabel(card, "Desc", item.desc, 15, new Color(0.84f, 0.82f, 0.76f),
                160f, 60f, 290f, 76f);

            if (item.type == "consumable")
                UIFactory.AddButton(card, "Use", "使用（+" + item.heal + "HP）", 24f, 165f, 200f, 54f,
                    () => Use(index), new Color(0.18f, 0.32f, 0.2f, 1f));
            else
                UIFactory.AddButton(card, "Equip", "装备到" + SlotName(item.type), 24f, 165f, 200f, 54f,
                    () => Equip(index), new Color(0.22f, 0.28f, 0.36f, 1f));
            UIFactory.AddButton(card, "Sell", "卖出 +" + Mathf.Max(1, item.price / 2),
                240f, 165f, 210f, 54f, () => Sell(index), new Color(0.36f, 0.22f, 0.16f, 1f));
        }
    }

    string IconKey(ItemData item)
    {
        if (item.type == "consumable")
            return item.heal >= 100 ? "potion_gold" : item.heal >= 50 ? "potion_purple"
                 : item.heal >= 25 ? "potion_green" : "bandage";
        if (item.type == "helmet") return "helmet";
        if (item.type == "armor") return "armor";
        if (item.type == "amulet") return "amulet";
        if (item.name.Contains("弓")) return "bow";
        if (item.name.Contains("弩")) return "crossbow";
        if (item.name.Contains("杖")) return "staff";
        if (item.name.Contains("书")) return "book";
        if (item.name.Contains("斧")) return "axe";
        if (item.name.Contains("锤")) return "hammer";
        return "sword";
    }

    string SlotName(string type)
    {
        switch (type)
        {
            case "weapon": return "武器";
            case "helmet": return "头盔";
            case "armor": return "护甲";
            default: return "护符";
        }
    }

    void Use(int index)
    {
        inventory.UseConsumable(index, health);
        RefreshPlayer();
        Rebuild();
    }

    void Equip(int index)
    {
        ItemData item = GameData.Item(inventory.Bag[index]);
        if (item == null) return;
        int slot = System.Array.IndexOf(InventoryComponent.Slots, item.type);
        if (slot < 0) return;
        inventory.EquipAt(index, slot);
        RefreshPlayer();
        Rebuild();
    }

    void Unequip(int slot)
    {
        inventory.Unequip(slot);
        RefreshPlayer();
        Rebuild();
    }

    void Sell(int index)
    {
        inventory.SellAt(index);
        RefreshPlayer();
        Rebuild();
    }

    void RefreshPlayer()
    {
        if (player == null) return;
        bonus.Refresh(inventory);
        player.RefreshGear();
    }
}
