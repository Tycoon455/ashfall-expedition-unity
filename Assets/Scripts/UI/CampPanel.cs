using UnityEngine;
using UnityEngine.UI;

public class CampPanel : MonoBehaviour
{
    public System.Action OnShop;
    public System.Action OnInventory;
    public System.Action OnSave;
    public System.Action OnNext;
    public System.Action OnMenu;

    RectTransform root;
    Text title;

    public void Build(Transform parent)
    {
        root = UIFactory.AddPanel(parent, "CampPanel", 430f, 180f, 740f, 560f,
            new Color(1f, 1f, 1f, 0.96f));
        title = UIFactory.AddLabel(root, "Title", "营地", 40,
            new Color(0.96f, 0.84f, 0.58f), 40f, 30f, 660f, 70f, TextAnchor.MiddleCenter,
            FontStyle.Bold);
        UIFactory.AddLabel(root, "Desc", "补给、整理装备，然后继续深入废土。\n生命不会自动恢复，请在商店购买医疗品。",
            20, new Color(0.86f, 0.82f, 0.74f), 60f, 110f, 620f, 90f, TextAnchor.MiddleCenter);

        UIFactory.AddButton(root, "Shop", "商店（B）", 80f, 220f, 260f, 62f,
            () => { if (OnShop != null) OnShop(); }, new Color(0.32f, 0.26f, 0.16f, 1f));
        UIFactory.AddButton(root, "Bag", "背包（I）", 400f, 220f, 260f, 62f,
            () => { if (OnInventory != null) OnInventory(); }, new Color(0.24f, 0.3f, 0.25f, 1f));
        UIFactory.AddButton(root, "Save", "存档", 80f, 310f, 260f, 62f,
            () => { if (OnSave != null) OnSave(); }, new Color(0.2f, 0.25f, 0.32f, 1f));
        UIFactory.AddButton(root, "Menu", "返回主菜单", 400f, 310f, 260f, 62f,
            () => { if (OnMenu != null) OnMenu(); }, new Color(0.3f, 0.2f, 0.2f, 1f));
        UIFactory.AddButton(root, "Next", "进入下一关", 180f, 440f, 380f, 72f,
            () => { if (OnNext != null) OnNext(); }, new Color(0.44f, 0.3f, 0.1f, 1f));
        root.gameObject.SetActive(false); // 默认隐藏，Show() 时打开
    }

    public void SetTitle(string text) { title.text = text; }
    public void Show(string text) { SetTitle(text); root.gameObject.SetActive(true); }
    public void Hide() { root.gameObject.SetActive(false); }
}
