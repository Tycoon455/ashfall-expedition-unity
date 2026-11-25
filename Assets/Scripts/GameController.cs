using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState { Menu, Level, Camp, GameOver, Victory }

/// <summary>营地检查点：进入营地那一刻的完整状态。死亡后回滚到此，回到营地重来。</summary>
[System.Serializable]
public class CampCheckpoint
{
    public string profession = "warrior";
    public int levelIndex0 = 0;
    public int gold = 0;
    public System.Collections.Generic.List<string> bag = new System.Collections.Generic.List<string>();
    public string[] equipped = { "", "", "", "" };
    public float hp;
    public float hpMax;
    public float stamina;
}

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    public GameState State = GameState.Menu;
    public int LevelIndex0;
    public string ProfessionId = "warrior";

    Canvas canvas;
    GameObject playerObj;
    PlayerController player;
    InventoryComponent inventory;
    SetBonusComponent bonus;
    LevelManager levels;
    HUD hud;
    MainMenu menu;
    CampPanel camp;
    CampMap campMap;
    SettingsPanel settings;
    ShopPanel shop;
    InventoryPanel bagPanel;

    /// <summary>供营地地图读取金币显示。</summary>
    public InventoryComponent Inventory { get { return inventory; } }
    /// <summary>供地面掉落物判断玩家是否走近拾取。</summary>
    public PlayerController Player { get { return player; } }
    RectTransform overlay;
    Text overlayTitle;
    string overlayButtonAction;
    bool panelsOpen;
    CampCheckpoint campCheckpoint;   // 进入营地时的快照，用于死亡回滚

    void Awake()
    {
        Instance = this;
        GameData.Load();
    }

    void Start()
    {
        SetupCamera();
        canvas = UIFactory.CreateCanvas();

        playerObj = new GameObject("Player");
        GameActorTag tag = playerObj.AddComponent<GameActorTag>();
        tag.professionId = ProfessionId;
        inventory = playerObj.AddComponent<InventoryComponent>();
        bonus = playerObj.AddComponent<SetBonusComponent>();
        player = playerObj.AddComponent<PlayerController>();

        levels = gameObject.AddComponent<LevelManager>();

        GameObject hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(transform, false);
        hud = hudGo.AddComponent<HUD>();
        hud.Build(canvas.transform);
        hud.OnSettings += OpenSettingsFromGame;

        GameObject menuGo = new GameObject("MainMenu");
        menuGo.transform.SetParent(transform, false);
        menu = menuGo.AddComponent<MainMenu>();
        menu.Build(canvas.transform);
        menu.OnNewGame += NewGame;
        menu.OnLoad += LoadGame;
        menu.OnSettings += () => { if (settings != null) settings.Open(); };

        GameObject campGo = new GameObject("CampPanel");
        campGo.transform.SetParent(transform, false);
        camp = campGo.AddComponent<CampPanel>();
        camp.Build(canvas.transform);
        camp.OnShop += OpenShop;
        camp.OnInventory += OpenBag;
        camp.OnSave += () => SaveGame(1);
        camp.OnNext += NextLevel;
        camp.OnMenu += ShowMenu;

        // 营地独立地图 + 设置面板
        GameObject campMapGo = new GameObject("CampMap");
        campMapGo.transform.SetParent(transform, false);
        campMap = campMapGo.AddComponent<CampMap>();
        campMap.Build(transform, this);

        GameObject settingsGo = new GameObject("SettingsPanel");
        settingsGo.transform.SetParent(transform, false);
        settings = settingsGo.AddComponent<SettingsPanel>();
        settings.Build(canvas.transform, ShowMenu);
        settings.OnSaveExit = SaveAndShowMenu;

        GameObject shopGo = new GameObject("ShopPanel");
        shopGo.transform.SetParent(transform, false);
        shop = shopGo.AddComponent<ShopPanel>();
        shop.Build(canvas.transform, ClosePanels);

        GameObject bagGo = new GameObject("InventoryPanel");
        bagGo.transform.SetParent(transform, false);
        bagPanel = bagGo.AddComponent<InventoryPanel>();
        bagPanel.Build(canvas.transform, ClosePanels);

        BuildOverlay();

        EventBus.PlayerDied += OnPlayerDied;
        EventBus.EnemyDied += OnEnemyDied;
        ShowMenu();
    }

    void OnDestroy()
    {
        EventBus.PlayerDied -= OnPlayerDied;
        EventBus.EnemyDied -= OnEnemyDied;
        Time.timeScale = 1f;
    }

    /// <summary>关卡金币倍率：随通关层级线性提升，第1关 ×1.0 → 第10关 ×3.4。</summary>
    float GoldMultiplier()
    {
        return 1f + Mathf.Clamp(LevelIndex0, 0, 9) * (2.4f / 9f);
    }

    /// <summary>击杀结算：金币必得（按关卡放大），装备按关卡品质权重掉落，Boss 保底；地面生成可见掉落物。</summary>
    void OnEnemyDied(EnemyController enemy)
    {
        if (enemy == null || enemy.Data == null || inventory == null) return;
        Vector3 at = enemy.transform.position;

        int killGold = Mathf.Max(1, Mathf.RoundToInt(enemy.Data.gold * GoldMultiplier()));
        inventory.Gold += killGold;
        EventBus.RaiseGoldChanged(inventory.Gold);
        LootDrop.Spawn(at, LootDrop.Kind.Gold, "", 0, "+" + killGold + " 金");

        // Boss 额外金币奖励：基础奖励同样随关卡放大
        if (enemy.Data.kind == "boss")
        {
            int bonus = Mathf.RoundToInt((enemy.Data.id == "boss_final" ? 200 : 80) * GoldMultiplier());
            inventory.Gold += bonus;
            EventBus.RaiseGoldChanged(inventory.Gold);
            LootDrop.Spawn(at, LootDrop.Kind.Gold, "", 0, "+" + bonus + " 金");
        }

        float chance = enemy.Data.kind == "boss" ? 1f
                     : enemy.Data.kind == "elite" ? 0.6f : 0.3f;
        if (Random.value > chance) return;

        int rarity = RollRarity(enemy.Data.kind);
        ItemData drop = PickDrop(rarity, enemy.Data.kind);
        if (drop == null) return;
        if (!inventory.Add(drop.id)) return;
        LootDrop.Spawn(at, LootDrop.Kind.Item, drop.id, drop.rarity,
            drop.name + " [" + GameConfig.RarityNames[drop.rarity] + "]");
        EventBus.RaiseMessage("掉落：" + drop.name + " [" + GameConfig.RarityNames[drop.rarity] + "]");
    }

    int RollRarity(string kind)
    {
        if (kind == "boss")
            return LevelIndex0 >= GameData.LevelCount - 1 ? 4 : 3;
        int[] weights = GameData.LootWeight(LevelIndex0);
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

    ItemData PickDrop(int rarity, string kind)
    {
        List<ItemData> pool = new List<ItemData>();
        foreach (ItemData item in GameData.ItemsByType(
            new string[] { "weapon", "helmet", "armor", "amulet" }[Random.Range(0, 4)], ProfessionId))
            if (item.rarity == rarity) pool.Add(item);
        if (pool.Count == 0)
        {
            foreach (ItemData item in GameData.ItemsByType("weapon", ProfessionId))
                if (item.rarity == rarity) pool.Add(item);
        }
        if (pool.Count == 0)
        {
            foreach (ItemData item in GameData.ItemsByType("weapon", ""))
                if (item.rarity == rarity) pool.Add(item);
        }
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            go.transform.position = new Vector3(8f, 0f, -10f);
        }
        cam.orthographic = true;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || player == null) return;
        float worldWidth = State == GameState.Camp ? CampMap.Width : Mathf.Max(6f, levels.WorldWidth);
        float minX = State == GameState.Camp ? 2.5f : 6f;
        float targetX = Mathf.Clamp(player.transform.position.x, minX, Mathf.Max(minX, worldWidth - minX));
        Vector3 p = cam.transform.position;
        p.x = Mathf.Lerp(p.x, targetX, 1f - Mathf.Pow(0.002f, Time.deltaTime));
        cam.transform.position = p;
    }

    void BuildOverlay()
    {
        overlay = UIFactory.Box(canvas.transform, "Overlay", 0f, 0f, 1600f, 900f);
        UIFactory.Image(overlay, new Color(0.04f, 0.05f, 0.06f, 0.9f));
        RectTransform card = UIFactory.AddPanel(overlay, "Card", 500f, 280f, 600f, 340f,
            new Color(1f, 1f, 1f, 0.97f));
        overlayTitle = UIFactory.AddLabel(card, "Title", "", 38,
            new Color(0.96f, 0.82f, 0.56f), 40f, 50f, 520f, 90f, TextAnchor.MiddleCenter,
            FontStyle.Bold);
        UIFactory.AddButton(card, "Primary", "继续", 150f, 200f, 300f, 68f,
            () => OverlayAction(), new Color(0.42f, 0.3f, 0.12f, 1f));
        overlay.gameObject.SetActive(false);
    }

    void OverlayAction()
    {
        if (overlayButtonAction == "retry")
        {
            // 战败：回滚到关卡开始前的营地状态，而非原地重开
            overlay.gameObject.SetActive(false);
            RestoreCampCheckpoint();
        }
        else if (overlayButtonAction == "menu")
        {
            overlay.gameObject.SetActive(false);
            ShowMenu();
        }
    }

    void Update()
    {
        // 统一返回键：Esc 只负责"退出当前层"，具体优先级见 HandleExitKey()
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleExitKey();
            return;
        }

        if (State != GameState.Menu && player != null && player.Health != null && player.Stamina != null)
            hud.Bind(player.Health, player.Stamina, LevelIndex0 + 1, GameData.LevelCount);

        // 清场出口：走到旁边只显示提示，必须按 E 才完成关卡
        bool atExit = State == GameState.Level && levels != null && levels.PlayerAtExit();
        if (hud != null) hud.ShowExitPrompt(atExit);
        if (atExit && Input.GetKeyDown(KeyCode.E)) CompleteLevel();

        // B / I 只负责打开；关闭统一走 Esc 或面板上的关闭按钮
        if (Input.GetKeyDown(KeyCode.B) && !panelsOpen && State == GameState.Camp) OpenShop();
        if (Input.GetKeyDown(KeyCode.I) && !panelsOpen &&
            (State == GameState.Camp || State == GameState.Level)) OpenBag();
        if (inventory != null && hud != null) hud.RefreshGold(inventory.Gold);
    }

    static int exitKeyFrame = -1; // 同帧去重：GetKeyDown 在同一帧的多个 Update 中都返回 true

    /// <summary>统一返回键：设置 → 面板 → 结算层 → 场景，逐层退出；无面板时保存并回主菜单。</summary>
    public void HandleExitKey()
    {
        // 只处理一帧一次，避免"关闭面板"和"回主菜单"在同帧连跳两级
        if (exitKeyFrame == Time.frameCount) return;
        exitKeyFrame = Time.frameCount;
        if (settings != null && settings.IsOpen) { settings.Close(); return; }
        if (panelsOpen) { ClosePanels(); return; }
        if (overlay != null && overlay.gameObject.activeSelf)
        {
            overlay.gameObject.SetActive(false);
            ShowMenu();
            return;
        }
        if (State == GameState.Camp || State == GameState.Level)
        {
            SaveGame(1);
            ShowMenu();
        }
    }

    public void NewGame(string professionId)
    {
        ProfessionId = professionId;
        playerObj.GetComponent<GameActorTag>().professionId = professionId;
        inventory.Gold = 0;
        inventory.Bag.Clear();
        for (int i = 0; i < InventoryComponent.Slots.Length; i++) inventory.Equipped[i] = "";
        // 开发者模式：直达最高关（第 10 关）
        LevelIndex0 = DevMode.Enabled ? GameData.LevelCount - 1 : 0;
        player.Setup(professionId, inventory, bonus, 1.5f);
        CaptureCampCheckpoint(); // 第1关无前置营地：以新游戏初始状态为回滚基线
        EnterLevel();
    }

    public void LoadGame(int slot)
    {
        SaveData data = SaveManager.Load(slot);
        if (data == null)
        {
            EventBus.RaiseMessage("该槽位没有存档");
            return;
        }
        ProfessionId = data.profession;
        playerObj.GetComponent<GameActorTag>().professionId = ProfessionId;
        int savedIndex = Mathf.Clamp(data.level - 1, 0, GameData.LevelCount - 1);
        LevelIndex0 = DevMode.Enabled ? GameData.LevelCount - 1 : savedIndex;
        inventory.LoadFrom(data);
        bonus.Refresh(inventory);
        player.Setup(ProfessionId, inventory, bonus, 1.5f);
        if (data.hp >= 0f && player.Health != null)
            player.Health.CurrentHp = Mathf.Clamp(data.hp, 1f, player.Health.MaxHp);
        if (data.stamina >= 0f && player.Stamina != null)
            player.Stamina.Current = Mathf.Clamp(data.stamina, 0f, player.Stamina.Max);
        CaptureCampCheckpoint(); // 载入状态即为本关前置营地基线
        // 回到存档点：营地存档回营地，关卡存档进关卡；不再一律推进到下一关
        if (data.inCamp) EnterCamp();
        else
        {
            EnterLevel();
            // 已清场的关卡直接保留出口，并回到存档时的位置，不用重打一遍
            if (data.levelCleared && levels != null) levels.RestoreCleared(true);
            if (data.playerX >= 0f && player != null && levels != null)
            {
                float x = Mathf.Clamp(data.playerX, 1.5f, Mathf.Max(1.5f, levels.WorldWidth - 1.5f));
                player.transform.position = new Vector3(x, player.transform.position.y, 0f);
            }
        }
    }

    public void SaveGame(int slot)
    {
        SaveData data = new SaveData();
        data.profession = ProfessionId;
        data.level = LevelIndex0 + 1;
        data.playerLevel = 1;
        data.inCamp = State == GameState.Camp;
        data.hp = player != null && player.Health != null ? player.Health.CurrentHp : -1f;
        data.stamina = player != null && player.Stamina != null ? player.Stamina.Current : -1f;
        data.levelCleared = State == GameState.Level && levels != null && levels.ExitReady;
        data.playerX = player != null ? player.transform.position.x : -1f;
        inventory.WriteTo(data);
        SaveManager.Save(slot, data);
    }

    /// <summary>快照"进入营地那一刻"的完整状态：金币/背包/装备/职业/关卡/血量体力。</summary>
    void CaptureCampCheckpoint()
    {
        campCheckpoint = new CampCheckpoint();
        campCheckpoint.profession = ProfessionId;
        campCheckpoint.levelIndex0 = LevelIndex0;
        campCheckpoint.gold = inventory.Gold;
        campCheckpoint.bag = new List<string>(inventory.Bag);
        campCheckpoint.equipped = (string[])inventory.Equipped.Clone();
        campCheckpoint.hpMax = player.Health.MaxHp;
        campCheckpoint.hp = player.Health.CurrentHp;
        campCheckpoint.stamina = player.Stamina.Current;
        Debug.Log("[检查点] 已快照营地状态：关卡" + (LevelIndex0 + 1) + " 金币" + campCheckpoint.gold +
                  " 背包" + campCheckpoint.bag.Count + " HP" + campCheckpoint.hp.ToString("F0"));
    }

    /// <summary>死亡回滚：恢复营地快照，回到关卡开始前的营地（而非重开当前关）。</summary>
    void RestoreCampCheckpoint()
    {
        if (campCheckpoint == null) { EnterLevel(); return; } // 无快照兜底：重开当前关

        ProfessionId = campCheckpoint.profession;
        playerObj.GetComponent<GameActorTag>().professionId = ProfessionId;
        LevelIndex0 = Mathf.Clamp(campCheckpoint.levelIndex0, 0, GameData.LevelCount - 1);

        inventory.Gold = campCheckpoint.gold;
        inventory.Bag.Clear();
        inventory.Bag.AddRange(campCheckpoint.bag);
        for (int i = 0; i < InventoryComponent.Slots.Length; i++)
            inventory.Equipped[i] = i < campCheckpoint.equipped.Length ? campCheckpoint.equipped[i] : "";
        EventBus.RaiseGoldChanged(inventory.Gold);

        // 重建玩家属性（血量上限/防御/套装由装备决定），再恢复到快照的血量与体力
        bonus.Refresh(inventory);
        player.Setup(ProfessionId, inventory, bonus, 2.4f);
        player.Health.CurrentHp = Mathf.Clamp(campCheckpoint.hp, 1f, player.Health.MaxHp);
        player.Stamina.Current = Mathf.Clamp(campCheckpoint.stamina, 0f, player.Stamina.Max);

        // 回到关卡开始前的营地
        EnterCamp();
        EventBus.RaiseMessage("战败回滚：已返回关卡开始前的营地，金币/装备/状态已恢复");
        EventBus.RaiseStateChanged();
    }

    /// <summary>进入营地独立地图：清掉关卡场景，关闭关卡 HUD，只显示营地自己的 UI。</summary>
    void EnterCamp()
    {
        ClosePanels();
        State = GameState.Camp;
        player.InputEnabled = false;
        if (levels != null) levels.Clear();
        if (campMap != null) campMap.Enter(player, LevelIndex0);
        // 关卡 HUD 与营地的操作提示都位于左下角，必须互斥显示，否则文字会重叠
        if (hud != null) hud.Show(false);
    }

    void EnterLevel()
    {
        ClosePanels();
        State = GameState.Level;
        menu.Hide();
        camp.Hide();
        if (campMap != null) campMap.Exit();
        hud.Show(true);
        bonus.Refresh(inventory);
        player.RefreshGear();
        player.InputEnabled = true;
        levels.Build(LevelIndex0, player, inventory, bonus);
        EventBus.RaiseStateChanged();
    }

    /// <summary>通关奖励：基础奖励随关卡层级提升，鼓励推进到高层营地。</summary>
    int ClearReward(int clearedLevel0)
    {
        return Mathf.RoundToInt((60f + clearedLevel0 * 25f) * (1f + Mathf.Clamp(clearedLevel0, 0, 9) * (2.4f / 9f)));
    }

    void CompleteLevel()
    {
        int clearedLevel = LevelIndex0;      // 刚通关的关卡（0-based）
        int clearGold = ClearReward(clearedLevel);
        if (inventory != null)
        {
            inventory.Gold += clearGold;
            EventBus.RaiseGoldChanged(inventory.Gold);
        }
        LevelIndex0++;
        if (LevelIndex0 >= GameData.LevelCount)
        {
            State = GameState.Victory;
            player.InputEnabled = false;
            hud.Show(false);
            overlayTitle.text = "废土通关\n你击败了深渊领主\n通关奖励 ◈ " + clearGold;
            overlayButtonAction = "menu";
            overlay.gameObject.SetActive(true);
            return;
        }
        // 统一入口：清掉关卡场景并隐藏关卡 HUD，再进入营地独立地图
        EnterCamp();
        // 先切到营地再存档，确保读取时回到这个营地存档点，而不是直接进下一关
        SaveGame(1);
        // 通关金币的可见反馈：在营地玩家位置生成金光宝珠
        if (player != null)
            LootDrop.Spawn(player.transform.position, LootDrop.Kind.Gold, "", 0, "通关奖励 +" + clearGold + " 金");
        // 快照营地状态：此刻起营地里的买卖/用药、关卡内所得都将在死亡时回滚到此点
        CaptureCampCheckpoint();
        EventBus.RaiseMessage("关卡完成 · 通关奖励 ◈ " + clearGold + "，自动存档已更新");
        EventBus.RaiseStateChanged();
    }

    void OnPlayerDied()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
        player.InputEnabled = false;
        overlayTitle.text = "战败\n返回关卡开始前的营地\n金币 / 装备 / 状态将回滚";
        overlayButtonAction = "retry";
        overlay.gameObject.SetActive(true);
    }

    public void NextLevel()
    {
        LevelIndex0 = Mathf.Clamp(LevelIndex0, 0, GameData.LevelCount - 1);
        EnterLevel();
    }

    /// <summary>游戏内 HUD 的设置按钮：暂停战斗，关闭后恢复到原状态。</summary>
    void OpenSettingsFromGame()
    {
        if (settings == null) return;
        if (panelsOpen) ClosePanels();
        if (player != null) player.InputEnabled = false;
        if (campMap != null) campMap.Paused = true;
        Time.timeScale = 0f;
        settings.OpenInGame(CloseSettingsFromGame);
    }

    void CloseSettingsFromGame()
    {
        Time.timeScale = 1f;
        if (player != null && (State == GameState.Level || State == GameState.Camp))
            player.InputEnabled = true;
        if (campMap != null)
        {
            campMap.Paused = false;
            if (State == GameState.Camp) campMap.RefreshInfo(LevelIndex0);
        }
    }

    public void OpenShop()
    {
        panelsOpen = true;
        player.InputEnabled = false;
        if (campMap != null) campMap.Paused = true;
        camp.Hide();
        shop.Open(inventory, LevelIndex0, ProfessionId, player.Health);
    }

    public void OpenBag()
    {
        panelsOpen = true;
        player.InputEnabled = false;
        if (campMap != null) campMap.Paused = true;
        camp.Hide();
        bagPanel.Open(inventory, bonus, player.Health, player);
    }

    void ClosePanels()
    {
        if (shop != null) shop.Hide();
        if (bagPanel != null) bagPanel.Hide();
        panelsOpen = false;
        if (player != null && State == GameState.Level) player.InputEnabled = true;
        if (campMap != null)
        {
            campMap.Paused = false;
            if (State == GameState.Camp) campMap.RefreshInfo(LevelIndex0);
        }
    }

    /// <summary>设置面板里的「保存并回主菜单」：先落盘当前进度，再回主菜单。</summary>
    public void SaveAndShowMenu()
    {
        if (State == GameState.Camp || State == GameState.Level) SaveGame(1);
        ShowMenu();
    }

    public void ShowMenu()
    {
        State = GameState.Menu;
        Time.timeScale = 1f;
        if (player != null) player.InputEnabled = false;
        if (levels != null) levels.Clear();
        hud.Show(false);
        camp.Hide();
        if (campMap != null) campMap.Exit();
        if (settings != null) settings.Hide();
        shop.Hide();
        bagPanel.Hide();
        overlay.gameObject.SetActive(false);
        panelsOpen = false;
        menu.Show();
    }
}
