using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    RectTransform root;
    RectTransform list;
    Text goldText;
    Button refreshBtn;
    InventoryComponent inventory;
    HealthComponent health;
    string profession;
    int refreshCount;      // 本次开店已刷新次数（仅统计，不影响固定价格）
    int currentLevel;      // 当前关卡序号（0-based）
    bool stockReady;       // 当前批次是否已生成；关闭重开不重掷
    string stockProfession;
    int stockLevel;
    readonly List<ItemData> stock = new List<ItemData>();

    /// <summary>刷新价格固定 5 金：不随关卡、刷新次数递增；开发者模式免费。</summary>
    int RefreshCost()
    {
        if (DevMode.FreePurchase) return 0;
        return 5;
    }

    public void Build(Transform parent, System.Action onClose)
    {
        root = UIFactory.AddPanel(parent, "ShopPanel", 250f, 90f, 1100f, 720f,
            new Color(1f, 1f, 1f, 0.98f));
        UIFactory.AddLabel(root, "Title", "废土营地 · 商店", 32,
            new Color(0.96f, 0.84f, 0.58f), 40f, 24f, 500f, 60f, TextAnchor.MiddleLeft,
            FontStyle.Bold);
        goldText = UIFactory.AddLabel(root, "Gold", "", 20, new Color(1f, 0.84f, 0.36f),
            545f, 30f, 165f, 44f, TextAnchor.MiddleRight, FontStyle.Bold);
        refreshBtn = UIFactory.AddButton(root, "Refresh", "刷新", 720f, 26f, 160f, 54f,
            () => DoRefresh(), new Color(0.2f, 0.32f, 0.42f, 1f));
        UIFactory.AddButton(root, "Close", "关闭（Esc）", 890f, 26f, 170f, 54f,
            () => { Hide(); if (onClose != null) onClose(); },
            new Color(0.34f, 0.18f, 0.16f, 1f));
        list = UIFactory.Box(root, "Goods", 40f, 100f, 1020f, 590f);
        root.gameObject.SetActive(false); // 默认隐藏，Open() 时打开
    }

    /// <summary>
    /// 打开商店：仅当首次进入 / 换了职业 / 换了关卡时才生成新货。
    /// 同一营地反复开关会保留上一批货，只有点「刷新」才重掷。
    /// </summary>
    public void Open(InventoryComponent inv, int levelIndex0, string professionId, HealthComponent hp)
    {
        inventory = inv;
        health = hp;
        profession = professionId;
        currentLevel = levelIndex0;
        refreshCount = 0;
        if (!stockReady || stockProfession != professionId || stockLevel != levelIndex0)
            RollStock();
        root.gameObject.SetActive(true);
        Rebuild(); // 只渲染现有库存，不重新随机
    }

    public void Hide() { root.gameObject.SetActive(false); }

    /// <summary>按品质权重抽一个稀有度（0灰 1绿 2蓝 3紫 4橙）。</summary>
    static int RollRarity(int[] weights)
    {
        int total = 0;
        foreach (int w in weights) total += w;
        int roll = Random.Range(0, Mathf.Max(1, total));
        int acc = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            acc += weights[i];
            if (roll < acc) return i;
        }
        return 0;
    }

    /// <summary>
    /// 抽取一批新货。品质按 GameData.ShopWeight(currentLevel) 决定；
    /// 某品质在该部位无货时，只向"更低品质"降级，绝不退化成任意品质，
    /// 否则低关卡灰装抽完会冒出紫橙装。
    /// </summary>
    void RollStock()
    {
        stock.Clear();
        stockProfession = profession;
        stockLevel = currentLevel;
        stockReady = true;

        stock.AddRange(GameData.Consumables());
        string[] types = { "weapon", "helmet", "armor", "amulet" };

        if (DevMode.Enabled)
        {
            // 开发者模式：本职业两套完整橙装（8 件），买齐即可激活 4 件套增益
            List<ItemData> orange = new List<ItemData>();
            foreach (string type in types)
                foreach (ItemData it in GameData.ItemsByType(type, profession))
                    if (it.rarity == 4) orange.Add(it);

            List<string> setIds = new List<string>();
            foreach (ItemData it in orange)
                if (!setIds.Contains(it.set_id)) setIds.Add(it.set_id);

            for (int n = 0; n < 2 && setIds.Count > 0; n++)
            {
                string sid = setIds[Random.Range(0, setIds.Count)];
                setIds.Remove(sid);
                foreach (string type in types)
                {
                    for (int k = 0; k < orange.Count; k++)
                    {
                        if (orange[k].set_id == sid && orange[k].type == type)
                        {
                            stock.Add(orange[k]);
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            // 普通模式：按商店专属品质权重抽 8 件，每件只抽一次避免重复
            int[] weights = GameData.ShopWeight(currentLevel);
            HashSet<string> used = new HashSet<string>();
            foreach (string type in types)
            {
                for (int n = 0; n < 2; n++)
                {
                    int rarity = RollRarity(weights);
                    List<ItemData> pool = new List<ItemData>();
                    // 目标品质无货时逐级降级（灰=0 最低），不向上抽取
                    for (int r = rarity; r >= 0; r--)
                    {
                        foreach (ItemData it in GameData.ItemsByType(type, profession))
                            if (it.rarity == r && !used.Contains(it.id)) pool.Add(it);
                        if (pool.Count > 0) break;
                    }
                    // 该部位所有品质都抽空才跳过（正常不会发生）
                    if (pool.Count == 0) continue;
                    ItemData pick = pool[Random.Range(0, pool.Count)];
                    used.Add(pick.id);
                    stock.Add(pick);
                }
            }
        }
    }

    /// <summary>只负责把当前库存渲染成 UI，不触碰随机逻辑。</summary>
    void Rebuild()
    {
        if (goldText != null && inventory != null)
            goldText.text = "◈ " + DevMode.GoldLabel(inventory.Gold);
        if (refreshBtn != null)
        {
            Text rt = refreshBtn.GetComponentInChildren<Text>();
            if (rt != null)
                rt.text = DevMode.FreePurchase ? "刷新（免费）" : "刷新 " + RefreshCost() + " 金";
        }
        for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);

        for (int i = 0; i < stock.Count && i < 13; i++)
        {
            ItemData item = stock[i];
            int col = i / 7;
            int row = i % 7;
            float x = col * 510f;
            float y = row * 82f;

            RectTransform card = UIFactory.Box(list, "Good_" + i, x, y, 490f, 74f);
            UIFactory.Image(card, new Color(0.1f, 0.11f, 0.12f, 0.82f),
                ResourceFactory.Sprite("UI/frame_" + GameConfig.RarityFiles[Mathf.Clamp(item.rarity, 0, 4)]));

            Sprite icon = ResourceFactory.Sprite(IconFor(item), 180f);
            RectTransform iconRt = UIFactory.Box(card, "Icon", 8f, 7f, 60f, 60f);
            UIFactory.Image(iconRt, Color.white, icon);

            string desc = item.type == "consumable"
                ? "回复 " + item.heal + " HP"
                : "攻击 +" + Mathf.RoundToInt(GameData.Stat(item, "atk")) +
                  " 防御 +" + Mathf.RoundToInt(GameData.Stat(item, "def"));
            UIFactory.AddLabel(card, "Name", item.name + "  [" + GameConfig.RarityNames[item.rarity] + "]",
                18, GameConfig.RarityColor(item.rarity), 76f, 6f, 280f, 30f);
            UIFactory.AddLabel(card, "Desc", desc, 14, new Color(0.82f, 0.8f, 0.75f),
                76f, 36f, 280f, 30f);
            UIFactory.AddButton(card, "Buy", DevMode.Enabled ? "免费" : item.price + " 金", 370f, 14f, 108f, 46f,
                () => Buy(item), new Color(0.36f, 0.28f, 0.12f, 1f));
        }
    }

    /// <summary>付费刷新：固定 5 金，仅此入口会重掷整批商品。</summary>
    void DoRefresh()
    {
        int cost = RefreshCost();
        if (!DevMode.FreePurchase && inventory != null && inventory.Gold < cost)
        {
            EventBus.RaiseMessage("金币不足，无法刷新（需要 " + cost + " 金）");
            return;
        }
        if (!DevMode.FreePurchase && inventory != null)
        {
            inventory.Gold -= cost;
            EventBus.RaiseGoldChanged(inventory.Gold);
        }
        refreshCount++;
        RollStock();
        Rebuild();
        EventBus.RaiseMessage(DevMode.FreePurchase ? "商店已免费刷新" : "商店已刷新（-" + cost + " 金）");
    }

    string IconFor(ItemData item)
    {
        if (item.type == "consumable")
        {
            if (item.heal >= 100) return "Icons/icon_potion_gold";
            if (item.heal >= 50) return "Icons/icon_potion_purple";
            if (item.heal >= 25) return "Icons/icon_potion_green";
            return "Icons/icon_bandage";
        }
        if (item.type == "weapon")
        {
            string n = item.name;
            if (n.Contains("弓")) return "Icons/icon_bow";
            if (n.Contains("弩")) return "Icons/icon_crossbow";
            if (n.Contains("杖") || n.Contains("书")) return n.Contains("书") ? "Icons/icon_book" : "Icons/icon_staff";
            if (n.Contains("斧")) return "Icons/icon_axe";
            if (n.Contains("锤")) return "Icons/icon_hammer";
            return "Icons/icon_sword";
        }
        if (item.type == "helmet") return "Icons/icon_helmet";
        if (item.type == "armor") return "Icons/icon_armor";
        return "Icons/icon_amulet";
    }

    void Buy(ItemData item)
    {
        if (inventory == null) return;
        if (inventory.Buy(item)) Rebuild(); // 只更新金币显示，不重掷商品
    }
}
