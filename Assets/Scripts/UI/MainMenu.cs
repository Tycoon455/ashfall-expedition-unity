using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public System.Action<string> OnNewGame;
    public System.Action<int> OnLoad;
    public System.Action OnSettings;

    RectTransform root;
    Text detail;
    Text devHint;
    string selected = "warrior";

    public void Build(Transform parent)
    {
        root = UIFactory.Box(parent, "MainMenu", 0f, 0f, 1600f, 900f);
        UIFactory.Image(root, new Color(0.05f, 0.06f, 0.07f, 0.94f),
            ResourceFactory.Sprite("Backgrounds/wasteland"));

        UIFactory.AddLabel(root, "Title", "灰烬远征 · 废土十关", 58,
            new Color(0.96f, 0.85f, 0.6f), 0f, 80f, 1600f, 90f, TextAnchor.MiddleCenter,
            FontStyle.Bold);
        UIFactory.AddLabel(root, "Sub", "选择职业，开始闯关", 24,
            new Color(0.82f, 0.8f, 0.74f), 0f, 170f, 1600f, 44f, TextAnchor.MiddleCenter);

        string[] ids = { "warrior", "ranger", "mage" };
        string[] names = { "战士", "游侠", "法师" };
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            ProfessionData p = GameData.Profession(id);
            float x = 260f + i * 370f;
            UIFactory.AddButton(root, "Prof_" + id, names[i], x, 250f, 300f, 64f,
                () => Select(id), new Color(0.28f, 0.25f, 0.21f, 0.95f));

            Sprite portrait = ResourceFactory.Sprite("Characters/" + id, 220f);
            RectTransform pr = UIFactory.Box(root, "Portrait_" + id, x + 70f, 330f, 160f, 190f);
            UIFactory.Image(pr, Color.white, portrait);
        }

        detail = UIFactory.AddLabel(root, "Detail", "", 20, new Color(0.9f, 0.88f, 0.8f),
            260f, 540f, 1080f, 70f, TextAnchor.MiddleCenter);
        Select("warrior");

        devHint = UIFactory.AddLabel(root, "DevHint", "", 16, new Color(0.94f, 0.5f, 0.4f),
            0f, 212f, 1600f, 30f, TextAnchor.MiddleCenter, FontStyle.Bold);

        UIFactory.AddButton(root, "Start", "开始新游戏", 600f, 630f, 300f, 70f,
            () => { if (OnNewGame != null) OnNewGame(selected); },
            new Color(0.42f, 0.3f, 0.12f, 1f));
        UIFactory.AddButton(root, "Settings", "设置", 920f, 630f, 220f, 70f,
            () => { if (OnSettings != null) OnSettings(); },
            new Color(0.26f, 0.24f, 0.2f, 1f));
        UIFactory.AddButton(root, "Exit", "退出游戏", 1160f, 630f, 220f, 70f,
            ExitGame, new Color(0.32f, 0.18f, 0.16f, 1f));

        UIFactory.AddLabel(root, "SlotsTitle", "读取存档", 22,
            new Color(0.92f, 0.84f, 0.64f), 260f, 730f, 200f, 40f);
        for (int i = 1; i <= GameConfig.SaveSlotCount; i++)
        {
            int slot = i;
            UIFactory.AddButton(root, "Slot" + i, SaveManager.Describe(i),
                260f + (i - 1) * 360f, 780f, 330f, 66f,
                () => { if (OnLoad != null) OnLoad(slot); },
                new Color(0.2f, 0.22f, 0.24f, 0.96f));
        }
    }

    /// <summary>构建版直接退出进程；编辑器里点按钮则停止播放，方便测试。</summary>
    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Select(string id)
    {
        selected = id;
        ProfessionData p = GameData.Profession(id);
        if (p != null)
            detail.text = p.name + "  ·  HP " + p.max_hp + "  ·  体力 " + p.max_stamina +
                          "  ·  攻击 " + p.attack + "\n" + p.desc + "  特色技能：" + p.skill;
    }

    public void Show()
    {
        root.gameObject.SetActive(true);
        if (devHint != null)
            devHint.text = DevMode.Enabled ? "开发者模式已开启 · 最高关卡 / 无限金币 / 橙装商店" : "";
        Transform setBtn = root.Find("Settings");
        if (setBtn != null && setBtn.Find("Text") != null)
            setBtn.Find("Text").GetComponent<Text>().text = DevMode.Enabled ? "设置 · DEV" : "设置";
        // 刷新槽位描述
        for (int i = 1; i <= GameConfig.SaveSlotCount; i++)
        {
            Transform t = root.Find("Slot" + i);
            if (t == null) continue;
            Text label = t.Find("Text") != null ? t.Find("Text").GetComponent<Text>() : null;
            if (label != null) label.text = SaveManager.Describe(i);
        }
    }

    public void Hide() { root.gameObject.SetActive(false); }
}
