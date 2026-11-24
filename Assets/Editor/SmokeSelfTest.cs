using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

/// <summary>
/// 批处理运行时自检：验证数据、公式、套装、存档、开发者模式等关键路径。
/// 用法（在 -batchmode -quit 下）：
///   Tuanjie.exe -batchmode -quit -projectPath ... -executeMethod SmokeSelfTest.Run -logFile ...
/// 结果写入项目根目录 smoke_result.txt，并输出到 Unity 日志。
/// </summary>
public static class SmokeSelfTest
{
    static int pass, fail;
    static readonly List<string> lines = new List<string>();

    static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { pass++; lines.Add("PASS  " + name + (detail == "" ? "" : "  [" + detail + "]")); }
        else { fail++; lines.Add("FAIL  " + name + (detail == "" ? "" : "  [" + detail + "]")); }
    }

    public static void Run()
    {
        pass = 0; fail = 0; lines.Clear();
        string outPath = Path.Combine(Directory.GetCurrentDirectory(), "smoke_result.txt");
        try
        {
            // 1) 数据加载
            GameData.Load();
            Check("GameData.Ready", GameData.Ready);
            Check("职业数=3", GameData.Profession("warrior") != null &&
                  GameData.Profession("mage") != null && GameData.Profession("ranger") != null);

            // 2) 关卡：10 关，越往后掉落品质权重越高
            Check("关卡数=10", GameData.LevelCount == 10, "实际=" + GameData.LevelCount);
            int[] w0 = GameData.LootWeight(0), w9 = GameData.LootWeight(9);
            Check("掉落权重长度=5", w0 != null && w0.Length == 5 && w9 != null && w9.Length == 5);
            double AvgTier(int[] w)
            {
                int total = 0, weighted = 0;
                for (int i = 0; i < w.Length; i++) { total += w[i]; weighted += w[i] * i; }
                return total == 0 ? 0 : (double)weighted / total;
            }
            Check("高关掉落品质权重更高", AvgTier(w9) > AvgTier(w0),
                  "L1=" + AvgTier(w0).ToString("F2") + " L10=" + AvgTier(w9).ToString("F2"));

            // 3) 套装：45 套，品质分布 3/6/9/12/15（每职业 灰1/绿2/蓝3/紫4/橙5 ×3 职业）
            Check("套装数=45", CountSets() == 45, "实际=" + CountSets());
            int[] rarityCount = new int[5];
            AccumulateSetRarity(rarityCount);
            bool distOk = rarityCount[0] == 3 && rarityCount[1] == 6 && rarityCount[2] == 9 &&
                          rarityCount[3] == 12 && rarityCount[4] == 15;
            Check("套装品质分布 3/6/9/12/15", distOk,
                  "灰绿蓝紫橙=" + string.Join(",", Array.ConvertAll(rarityCount, x => x.ToString())));

            // 4) 伤势联动公式
            float cap(int hpPct) => GameConfig.CapacityMinMult + GameConfig.CapacityHpWeight * hpPct;
            float reg(int hpPct) => GameConfig.RegenMinMult + GameConfig.RegenHpWeight * hpPct;
            Check("满血体力上限=1.0", Mathf.Approximately(cap(1), 1f), cap(1).ToString("F2"));
            Check("空血体力上限=0.5", Mathf.Approximately(cap(0), 0.5f), cap(0).ToString("F2"));
            Check("满血体力回复=1.0", Mathf.Approximately(reg(1), 1f), reg(1).ToString("F2"));
            Check("空血体力回复=0.4", Mathf.Approximately(reg(0), 0.4f), reg(0).ToString("F2"));

            // 5) 开发者模式：只改显示与扣费，不改真实金币数值
            bool orig = DevMode.Enabled;
            DevMode.Enabled = false;
            Check("普通模式金币标签为数字", DevMode.GoldLabel(123) == "123");
            DevMode.Enabled = true;
            Check("开发者模式金币标签=∞", DevMode.GoldLabel(123) == "∞");
            Check("开发者模式免费购买", DevMode.FreePurchase);
            DevMode.Enabled = orig; // 还原，不污染用户设置

            // 6) 存档往返
            SaveData sd = new SaveData();
            sd.profession = "mage"; sd.level = 5; sd.gold = 777;
            sd.bag.Add("potion_hp"); sd.equipped[0] = "w_blue_01";
            SaveManager.Save(99, sd);
            SaveData back = SaveManager.Load(99);
            Check("存档往返-职业", back != null && back.profession == "mage");
            Check("存档往返-关卡", back != null && back.level == 5);
            Check("存档往返-金币", back != null && back.gold == 777);
            Check("存档往返-背包", back != null && back.bag.Contains("potion_hp"));
            SaveManager.Delete(99);
            Check("删除测试存档", !SaveManager.Has(99));

            // 7) 关键 Sprite 素材可加载
            Check("战士精灵可加载", Resources.Load<Texture2D>("Art/Characters/warrior") != null);
            Check("营地背景可加载", Resources.Load<Texture2D>("Art/Backgrounds/camp") != null);

            // 8) 显示设置：模式/分辨率/UI 缩放可持久化，且 UI 缩放不改模型尺寸
            DisplayPreset oldPreset = DisplaySettings.Preset;
            int oldW = DisplaySettings.Width, oldH = DisplaySettings.Height;
            float oldScale = DisplaySettings.UiScale;

            DisplaySettings.Preset = DisplayPreset.Windowed;
            Check("显示模式持久化-窗口化", DisplaySettings.Preset == DisplayPreset.Windowed);
            DisplaySettings.Preset = DisplayPreset.BorderlessFullscreen;
            Check("显示模式映射-无边框",
                  DisplaySettings.UnityMode(DisplayPreset.BorderlessFullscreen) == FullScreenMode.FullScreenWindow);
            Check("显示模式映射-全屏独占",
                  DisplaySettings.UnityMode(DisplayPreset.ExclusiveFullscreen) == FullScreenMode.ExclusiveFullScreen);
            bool has1080p = false;
            foreach (var r in DisplaySettings.Resolutions)
                if (r.width == 1920 && r.height == 1080) has1080p = true;
            Check("分辨率列表含1920×1080", has1080p);

            DisplaySettings.SetUiScale(1.2f, false);
            GameObject scalerGo = new GameObject("__ui_scale_test__", typeof(RectTransform), typeof(CanvasScaler));
            CanvasScaler cs = scalerGo.GetComponent<CanvasScaler>();
            DisplaySettings.ApplyTo(cs);
            Check("UI缩放写入参考分辨率",
                  Mathf.Abs(cs.referenceResolution.x - 1600f / 1.2f) < 0.01f,
                  cs.referenceResolution.x.ToString("F1"));
            UnityEngine.Object.DestroyImmediate(scalerGo);

            // 设置面板布局自检：新增显示设置后不能溢出 1600×900 参考画布
            GameObject uiCanvasGo = new GameObject("__settings_canvas__",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            GameObject settingsGo = new GameObject("__settings_panel__");
            SettingsPanel sp = settingsGo.AddComponent<SettingsPanel>();
            sp.Build(uiCanvasGo.transform, null);
            RectTransform settingsRoot = uiCanvasGo.transform.Find("SettingsPanel") as RectTransform;
            Check("设置面板尺寸820×840",
                  settingsRoot != null && settingsRoot.sizeDelta == new Vector2(820f, 840f));
            RectTransform backRt = settingsRoot != null ? settingsRoot.Find("Back") as RectTransform : null;
            Check("设置返回按钮在面板内",
                  backRt != null && backRt.anchoredPosition.x >= 0f &&
                  backRt.anchoredPosition.x + backRt.sizeDelta.x <= settingsRoot.sizeDelta.x &&
                  -backRt.anchoredPosition.y >= 0f &&
                  -backRt.anchoredPosition.y + backRt.sizeDelta.y <= settingsRoot.sizeDelta.y);
            RectTransform modeRow = settingsRoot != null ? settingsRoot.Find("ModeRow") as RectTransform : null;
            Check("窗口模式三按钮齐全",
                  modeRow != null && modeRow.Find("Mode0") != null &&
                  modeRow.Find("Mode1") != null && modeRow.Find("Mode2") != null);
            sp.Hide();
            UnityEngine.Object.DestroyImmediate(settingsGo);
            UnityEngine.Object.DestroyImmediate(uiCanvasGo);
            EventSystem es = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (es != null) UnityEngine.Object.DestroyImmediate(es.gameObject);

            // 9) 抠图复核：角色/敌人/道具不应存在大片中性白残留
            float worstWhiteRatio = 0f;
            string artRoot = Path.Combine(Application.dataPath, "Resources", "Art");
            foreach (string dirName in new[] { "Characters", "Enemies", "Props" })
            {
                string dir = Path.Combine(artRoot, dirName);
                foreach (string path in Directory.GetFiles(dir, "*.png"))
                {
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(File.ReadAllBytes(path))) continue;
                    Color32[] px = tex.GetPixels32();
                    int white = 0, opaque = 0;
                    for (int i = 0; i < px.Length; i++)
                    {
                        Color32 c = px[i];
                        if (c.a < 200) continue;
                        opaque++;
                        int mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                        int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        if (mn > 195 && mx - mn < 14) white++;
                    }
                    if (opaque > 0)
                        worstWhiteRatio = Mathf.Max(worstWhiteRatio, (float)white / opaque);
                    UnityEngine.Object.DestroyImmediate(tex);
                }
            }
            Check("角色/敌人/道具无大片残留白底", worstWhiteRatio < 0.02f,
                  "最大白色占比=" + worstWhiteRatio.ToString("P2"));

            // 图标抠图：重点复核曾残留白底的弓/弩（草药的浅色花穗是合理高光，不在此列）
            float worstIconWhite = 0f;
            string iconDir = Path.Combine(artRoot, "Icons");
            foreach (string name in new[] { "icon_bow.png", "icon_crossbow.png" })
            {
                string path = Path.Combine(iconDir, name);
                if (!File.Exists(path)) continue;
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(path))) continue;
                Color32[] px = tex.GetPixels32();
                int white = 0, opaque = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 c = px[i];
                    if (c.a < 200) continue;
                    opaque++;
                    int mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (mn > 195 && mx - mn < 14) white++;
                }
                if (opaque > 0)
                    worstIconWhite = Mathf.Max(worstIconWhite, (float)white / opaque);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            Check("装备图标（弓/弩）无残留白底", worstIconWhite < 0.005f,
                  "最大白色占比=" + worstIconWhite.ToString("P2"));

            // 10) 三职业攻击节奏与射程排序
            GameObject combatGo = new GameObject("__combat_profile_test__");
            CombatComponent combat = combatGo.AddComponent<CombatComponent>();
            combat.SetProfessionProfile("ranger", 3.2f);
            float rInterval = combat.AttackInterval, rRange = combat.AttackRange, rCd = combat.SkillCooldown;
            combat.SetProfessionProfile("warrior", 4f);
            float wInterval = combat.AttackInterval, wRange = combat.AttackRange, wCd = combat.SkillCooldown;
            combat.SetProfessionProfile("mage", 5f);
            float mInterval = combat.AttackInterval, mRange = combat.AttackRange, mCd = combat.SkillCooldown;
            Check("攻速排序 游侠>战士>法师",
                  rInterval < wInterval && wInterval < mInterval,
                  rInterval + "/" + wInterval + "/" + mInterval);
            Check("射程排序 游侠>法师>战士近战",
                  rRange > mRange && mRange > wRange * 1.7f,
                  rRange + "/" + mRange + "/" + (wRange * 1.7f).ToString("F2"));
            Check("战士近战保持加长范围", Mathf.Abs(wRange * 1.7f - 2.55f) < 0.01f,
                  (wRange * 1.7f).ToString("F2"));
            Check("职业技能冷却按职业保留",
                  Mathf.Approximately(rCd, 3.2f) && Mathf.Approximately(wCd, 4f) && Mathf.Approximately(mCd, 5f));
            // 闪避：冲刺距离与无敌覆盖
            float oldDodgeDist = combat.DodgeDistanceMult;
            combat.DodgeDistanceMult = 1f;
            float dashDist = 4f * GameConfig.DodgeSpeedMult * GameConfig.DodgeDuration;
            float iframe = combat.IFrameForDodge(null, GameConfig.DodgeDuration);
            Check("闪避冲刺距离明显大于旧值", dashDist > 5.5f, "冲刺≈" + dashDist.ToString("F2") + " 世界单位");
            Check("无敌覆盖冲刺全程并留缓冲",
                  iframe >= GameConfig.DodgeDuration + GameConfig.DodgeIFramePadding,
                  "无敌" + iframe.ToString("F2") + "s ≥ 冲刺" + GameConfig.DodgeDuration.ToString("F2") + "s");
            combat.DodgeDistanceMult = oldDodgeDist;
            string playerSrc2 = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Gameplay/PlayerController.cs"));
            Check("单按 K 也朝朝向冲刺（不原地空闪）",
                  playerSrc2.Contains("if (dodging) h = dodgeDir") &&
                  playerSrc2.Contains("dodgeDir = Mathf.Abs(h) > 0.01f"));
            UnityEngine.Object.DestroyImmediate(combatGo);

            // 11) 主菜单退出按钮 + 血条/体力条结构
            GameObject menuCanvasGo = new GameObject("__menu_canvas__",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            GameObject menuGo = new GameObject("__main_menu__");
            MainMenu menu = menuGo.AddComponent<MainMenu>();
            menu.Build(menuCanvasGo.transform);
            Check("主菜单含退出游戏按钮", menuCanvasGo.transform.Find("MainMenu/Exit") != null);
            UnityEngine.Object.DestroyImmediate(menuGo);
            UnityEngine.Object.DestroyImmediate(menuCanvasGo);

            GameObject hudCanvasGo = new GameObject("__hud_canvas__",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            GameObject hudGo = new GameObject("__hud__");
            HUD hud = hudGo.AddComponent<HUD>();
            hud.Build(hudCanvasGo.transform);
            RectTransform hpFillRt = hudCanvasGo.transform.Find("HUD/HPBar/Fill") as RectTransform;
            RectTransform stFillRt = hudCanvasGo.transform.Find("HUD/StaminaBar/Fill") as RectTransform;
            Check("玩家血条体力条分层结构存在",
                  hpFillRt != null && stFillRt != null &&
                  hpFillRt.parent.Find("Glow") != null && hpFillRt.parent.Find("Shine") != null &&
                  stFillRt.parent.Find("Glow") != null && stFillRt.parent.Find("Shine") != null);
            if (hpFillRt != null)
            {
                object hpWidget = typeof(HUD).GetField("hpBar",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(hud);
                var setTarget = hpWidget.GetType().GetMethod("SetTarget");
                var tick = hpWidget.GetType().GetMethod("Tick");
                setTarget.Invoke(hpWidget, new object[] { 0.5f });
                tick.Invoke(hpWidget, new object[] { 1f });
                float halfW = hpFillRt.sizeDelta.x;
                setTarget.Invoke(hpWidget, new object[] { 1f });
                tick.Invoke(hpWidget, new object[] { 1f });
                float fullW = hpFillRt.sizeDelta.x;
                Check("血条随扣血缩短并随回血伸长",
                      halfW < fullW * 0.55f && fullW > halfW * 1.8f,
                      halfW.ToString("F0") + " -> " + fullW.ToString("F0"));
            }
            RectTransform exitPromptRt = hudCanvasGo.transform.Find("HUD/ExitPrompt") as RectTransform;
            Check("出口 E 提示节点存在", exitPromptRt != null && exitPromptRt.Find("Text") != null);
            if (exitPromptRt != null)
            {
                hud.ShowExitPrompt(true);
                bool shown = exitPromptRt.gameObject.activeSelf;
                hud.ShowExitPrompt(false);
                Check("出口提示随靠近范围显隐", shown && !exitPromptRt.gameObject.activeSelf);
            }
            UnityEngine.Object.DestroyImmediate(hudGo);
            UnityEngine.Object.DestroyImmediate(hudCanvasGo);

            // 12) 怪物头顶血条同样随扣血缩短、回血伸长
            GameObject enemyBarGo = new GameObject("__enemy_bar__");
            EnemyController enemyBar = enemyBarGo.AddComponent<EnemyController>();
            EnemyData ed = new EnemyData
            {
                id = "grunt", name = "测试步兵", kind = "grunt", hp = 100,
                damage = 5, speed = 1, gold = 1, scale = 1f,
                color = new[] { 0.5f, 0.5f, 0.5f }
            };
            enemyBar.Setup(ed, null, 0f, 0f);
            Transform monsterFill = enemyBarGo.transform.Find("HPBar/Fill");
            float mFull = monsterFill == null ? 0f : monsterFill.localScale.x;
            enemyBar.Health.TakeDamage(50f);
            var shownField = typeof(EnemyController).GetField("hpShown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var applyBar = typeof(EnemyController).GetMethod("ApplyHealthBar",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            shownField.SetValue(enemyBar, 0.5f);
            applyBar.Invoke(enemyBar, null);
            float mHalf = monsterFill == null ? 0f : monsterFill.localScale.x;
            shownField.SetValue(enemyBar, 1f);
            applyBar.Invoke(enemyBar, null);
            float mRegrow = monsterFill == null ? 0f : monsterFill.localScale.x;
            Check("怪物血条随扣血缩短并伸长",
                  mHalf < mFull * 0.55f && mRegrow > mHalf * 1.8f,
                  mFull.ToString("F2") + " -> " + mHalf.ToString("F2") + " -> " + mRegrow.ToString("F2"));
            EnemyController.All.Remove(enemyBar);
            UnityEngine.Object.DestroyImmediate(enemyBarGo);

            // 13) 出口 E 交互 / 统一 Esc 返回 / 商店固定刷新价（源码级断言）
            string gcSrc = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/GameController.cs"));
            Check("出口必须按 E 才传送",
                  gcSrc.Contains("atExit && Input.GetKeyDown(KeyCode.E)") &&
                  gcSrc.Contains("hud.ShowExitPrompt(atExit)"));
            Check("统一 Esc 返回键实现",
                  gcSrc.Contains("public void HandleExitKey()") &&
                  gcSrc.Contains("if (Input.GetKeyDown(KeyCode.Escape))"));
            int enterCampAt = gcSrc.IndexOf("void EnterCamp()");
            string enterCampSrc = enterCampAt >= 0
                ? gcSrc.Substring(enterCampAt, Mathf.Min(700, gcSrc.Length - enterCampAt)) : "";
            Check("营地与关卡 HUD 互斥，不重叠左下角操作提示",
                  enterCampSrc.Contains("campMap.Enter") && enterCampSrc.Contains("hud.Show(false)"));
            string campSrc = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Gameplay/CampMap.cs"));
            Check("营地不重复监听 Esc + 同帧去重",
                  !campSrc.Contains("KeyCode.Escape") &&
                  gcSrc.Contains("exitKeyFrame == Time.frameCount"));
            string shopSrc = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/ShopPanel.cs"));
            Check("商店刷新固定 5 金", shopSrc.Contains("return 5;") &&
                  !shopSrc.Contains("refreshCount * 25") && !shopSrc.Contains("currentLevel * 10"));

            // 还原用户设置，不污染正在运行的编辑器 / 玩家配置
            DisplaySettings.Preset = oldPreset;
            DisplaySettings.SetResolution(oldW, oldH, false);
            DisplaySettings.SetUiScale(oldScale, false);
        }
        catch (Exception e)
        {
            fail++;
            lines.Add("FAIL  自检异常: " + e);
        }

        string header = "SMOKE TEST  PASS=" + pass + "  FAIL=" + fail + "  " + DateTime.Now.ToString("s");
        string content = header + "\n" + string.Join("\n", lines) + "\n";
        File.WriteAllText(outPath, content);
        Debug.Log("[SmokeSelfTest]\n" + content);
        if (fail > 0)
            Debug.LogError("[SmokeSelfTest] 存在 " + fail + " 项失败");
    }

    static int CountSets()
    {
        // 通过反射读取私有数据库计数不可行，改用遍历已知物品 set_id 去重
        var ids = new HashSet<string>();
        for (int r = 0; r < 5; r++)
            foreach (var item in AllItems())
                if (item.rarity == r && !string.IsNullOrEmpty(item.set_id)) ids.Add(item.set_id);
        return ids.Count;
    }

    static void AccumulateSetRarity(int[] counts)
    {
        var seen = new HashSet<string>();
        foreach (var item in AllItems())
        {
            if (string.IsNullOrEmpty(item.set_id) || seen.Contains(item.set_id)) continue;
            // 同一 set 内品质一致，用第一个物品的品质归属
            seen.Add(item.set_id);
            if (item.rarity >= 0 && item.rarity < counts.Length) counts[item.rarity]++;
        }
    }

    static IEnumerable<ItemData> AllItems()
    {
        // ItemsByType(type, "") 会过滤掉带职业的装备，因此逐职业取
        foreach (string prof in new[] { "warrior", "mage", "ranger" })
            foreach (string type in new[] { "weapon", "helmet", "armor", "amulet" })
                foreach (var it in GameData.ItemsByType(type, prof))
                    yield return it;
        foreach (var it in GameData.Consumables())
            yield return it;
    }
}
