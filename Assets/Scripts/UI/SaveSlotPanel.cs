using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 营地记忆石碑的存档槽面板：复用主菜单的三个槽位，先选择槽位再点保存。
/// 实际写盘由 GameController 的回调完成，因此之后从主菜单读取该槽会回到保存时的进度。
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    public System.Action<int> OnSave;
    public System.Action OnClose;

    RectTransform root;
    Button[] slotButtons;
    Text[] slotLabels;
    Text detailText;
    Text statusText;
    int selected = 1;

    readonly Color normalBtn = new Color(0.2f, 0.22f, 0.24f, 0.96f);
    readonly Color activeBtn = new Color(0.24f, 0.38f, 0.46f, 1f);

    public bool IsOpen { get { return root != null && root.gameObject.activeSelf; } }
    public int SelectedSlot { get { return selected; } }

    public void Build(Transform parent)
    {
        root = UIFactory.AddPanel(parent, "SaveSlotPanel", 430f, 150f, 740f, 600f,
            new Color(1f, 1f, 1f, 0.97f));

        UIFactory.AddLabel(root, "Title", "记忆石碑 · 保存进度", 34,
            new Color(0.96f, 0.84f, 0.58f), 30f, 20f, 680f, 58f,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFactory.AddLabel(root, "Desc", "选择一个存档槽，确认后写入当前进度；回主菜单读取该槽即可回到这里。",
            16, new Color(0.66f, 0.62f, 0.55f), 30f, 76f, 680f, 36f, TextAnchor.MiddleCenter);

        int count = GameConfig.SaveSlotCount;
        slotButtons = new Button[count];
        slotLabels = new Text[count];
        for (int i = 1; i <= count; i++)
        {
            int slot = i;
            slotButtons[i - 1] = UIFactory.AddButton(root, "Slot" + i, "",
                60f, 124f + (i - 1) * 92f, 620f, 78f,
                () => Select(slot), normalBtn);
            slotLabels[i - 1] = slotButtons[i - 1].transform.Find("Text").GetComponent<Text>();
            slotLabels[i - 1].fontSize = 17;
        }

        detailText = UIFactory.AddLabel(root, "Detail", "", 16,
            new Color(0.86f, 0.84f, 0.76f), 60f, 404f, 620f, 56f, TextAnchor.MiddleLeft);
        statusText = UIFactory.AddLabel(root, "Status", "", 17,
            new Color(0.56f, 0.86f, 0.56f), 60f, 454f, 620f, 40f,
            TextAnchor.MiddleCenter, FontStyle.Bold);

        UIFactory.AddButton(root, "SaveBtn", "保存到该槽位", 60f, 506f, 300f, 62f,
            DoSave, new Color(0.24f, 0.42f, 0.26f, 1f));
        UIFactory.AddButton(root, "CancelBtn", "取消（Esc）", 380f, 506f, 300f, 62f,
            Cancel, new Color(0.34f, 0.18f, 0.16f, 1f));

        root.gameObject.SetActive(false);
    }

    /// <summary>打开面板：刷新三槽描述并置顶，避免被结算层或其他面板遮住。</summary>
    public void Open(System.Action<int> onSave, System.Action onClose)
    {
        OnSave = onSave;
        OnClose = onClose;
        Refresh();
        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
    }

    void Refresh()
    {
        selected = Mathf.Clamp(selected, 1, GameConfig.SaveSlotCount);
        for (int i = 1; i <= GameConfig.SaveSlotCount; i++)
        {
            slotLabels[i - 1].text = "槽位 " + i + "　" + SaveManager.Describe(i);
        }
        UpdateSelectionVisual();
        if (detailText != null)
            detailText.text = "当前选中：槽位 " + selected + "　·　已有存档会被覆盖。";
        if (statusText != null) statusText.text = "";
    }

    void Select(int slot)
    {
        selected = slot;
        UpdateSelectionVisual();
        if (detailText != null)
            detailText.text = "当前选中：槽位 " + selected + "　·　已有存档会被覆盖。";
        if (statusText != null) statusText.text = "";
    }

    void UpdateSelectionVisual()
    {
        if (slotButtons == null) return;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;
            slotButtons[i].GetComponent<Image>().color =
                i + 1 == selected ? activeBtn : normalBtn;
        }
    }

    void DoSave()
    {
        if (OnSave != null) OnSave(selected);
        if (statusText != null)
            statusText.text = "已保存到槽位 " + selected + "　·　按 Esc 或「取消」返回营地";
        RefreshLabelsOnly();
    }

    void RefreshLabelsOnly()
    {
        for (int i = 1; i <= GameConfig.SaveSlotCount; i++)
            slotLabels[i - 1].text = "槽位 " + i + "　" + SaveManager.Describe(i);
        UpdateSelectionVisual();
    }

    void Cancel()
    {
        Hide();
        if (OnClose != null) OnClose();
    }

    public void Hide() { if (root != null) root.gameObject.SetActive(false); }
}
