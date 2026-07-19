using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CleanRouletteSceneBuilder
{
    const string SourceScenePath = "Assets/Scenes/TestRoullete.unity";
    const string GeneratedDir = "Assets/UI/Generated";
    const string PrefabDir = "Assets/Prefabs/UI/Clean";
    const string BasalFontPath = "Assets/UI/FONTS/Basal SDF.asset";

    // Everything visual lives here - same scene, same wiring, different skin
    class Theme
    {
        public string name;
        public string scenePath;
        public string assetSub; // "" keeps the original Obsidian asset paths
        public string fontPath;
        public float cornerRadius;
        public float bevel;
        public Color background, bgTop, bgBottom;
        public Color fillTop, fillBottom;
        public Color framePrimary, frameSecondary, frameDisabled;
        public Color slotFill;
        public Color textPrimary, textSecondary;
        public Color accent, accentBright, accentPressed;
    }

    static readonly Color CommonColor = Hex("C8D0DC");
    static readonly Color EpicColor = Hex("C070F8");
    static readonly Color LegendaryColor = Hex("F8A030");

    static Theme T;
    static Sprite raisedFrameSprite;
    static Sprite insetFrameSprite;
    static Sprite fillSprite;
    static Sprite circleSprite;
    static Sprite gradientSprite;
    static Sprite radialSprite;
    static Sprite barFillSprite;
    static Sprite[] speedIconSprites;
    static Sprite pauseIconSprite;
    static GameObject pipPrefab;
    static TMP_FontAsset font;

    // ---------- theme ----------

    // Arcane: deep violet windows, lavender-silver frames, cyan accents, hard bevel
    static Theme ArcaneTheme() => new Theme
    {
        name = "Arcane",
        scenePath = "Assets/Scenes/TestRouletteClean.unity",
        assetSub = "",
        fontPath = BasalFontPath,
        cornerRadius = 6f,
        bevel = 0.40f,
        background = Hex("08050E"),
        bgTop = Hex("120A20"), bgBottom = Hex("040208"),
        fillTop = Hex("241440"), fillBottom = Hex("0E081E"),
        framePrimary = Hex("A88CD8"), frameSecondary = Hex("5E4E86"), frameDisabled = Hex("564C70"),
        slotFill = Hex("120A24"),
        textPrimary = Hex("F0EAFF"), textSecondary = Hex("A99BC8"),
        accent = Hex("58D8E8"), accentBright = Hex("90F0FF"), accentPressed = Hex("2E98A8")
    };

    [MenuItem("Tools/Arkadian Skies/Build Clean Roulette Scene")]
    public static void BuildClean() => Build(ArcaneTheme(), true);

    [MenuItem("Tools/Arkadian Skies/Build Clean Battle Scene")]
    public static void BuildBattle() => BuildBattleScene();

    // ---------- build ----------

    class SlotPrefabs
    {
        public GameObject rouletteSlot;
        public ChampionSlotUI championSlot;
        public ItemSlotUI itemSlot;
    }

    class UIRefs
    {
        public Canvas canvas;
        public TMP_Text goldLabel;
        public Button openPartyButton, openBenchButton, openBankButton, openShopButton;
        public GameObject partyPanel, benchPanel, bankPanel, shopPanel;
        public Transform frontlineParent, backlineParent, benchSlotsParent, bankSlotsParent;
        public Transform rouletteSlotsParent;
        public Button commonButton, epicButton, legendaryButton, spinButton;
        public TMP_Text spinCostLabel;
        public TMP_Text feedbackLabel;
        public RectTransform goldChip;
        public TMP_Text floatingText;
        public Image dragGhost;

        public GameObject infoPanelRoot;
        public Button infoCloseButton;
        public Image infoPortrait;
        public TMP_Text infoName, infoLevel, infoMemento;
        public TMP_Text hp, ad, ap, atkSpd, crit, def, mdef, resource;
        public AbilityRowUI frontAbilityRow, backAbilityRow;
        public EquippedItemSlotUI[] equipSlots;

        public GameObject tooltipRoot;
        public TMP_Text tooltipTitle, tooltipDescription;

        public DialogueUIController dialogue;

        public Button pauseButton, resumeButton;
        public TMP_Text pauseLabel;
        public GameObject pausedBanner;
        public CanvasGroup bankGroup;

        public TMP_Text clockLabel, announceLabel;
        public RectTransform focusFrame, popupTemplate;
        public GameObject resultPanel;
        public TMP_Text resultTitle, resultSubtitle;
        public Button retryButton;

        public GameObject globalPanel;
        public CanvasGroup globalGroup;
        public GlobalItemSlotUI[] globalSlots;
        public Button openGlobalsButton;
        public TMP_Text globalMessage;
        public TMP_Text pauseChargesLabel;
        public TMP_Text pauseMessage;
        public Button[] speedButtons;

        public GameObject recipePanelRoot;
        public TMP_Text recipeTitle, recipeEmptyLabel;
        public Button recipeCloseButton;
        public ItemRecipeRowUI[] recipeRows;

        public GameObject otherPanel;
        public OtherItemSlotUI[] otherSlots;
        public Button openOthersButton;
        public GameObject artisanPanelRoot;
        public Button artisanCloseButton;
        public ArtisanAspectPanelUI.Choice[] artisanChoices;
    }

    static void Build(Theme theme, bool askToSave)
    {
        if (!File.Exists(SourceScenePath))
        {
            Debug.LogError($"Source scene not found at {SourceScenePath}");
            return;
        }
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        T = theme;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(T.fontPath);
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BasalFontPath);
        if (font == null)
        {
            Debug.LogWarning("No custom font asset found, falling back to TMP default.");
            font = TMP_Settings.defaultFontAsset;
        }

        GenerateSprites();

        if (SceneManager.GetActiveScene().path == T.scenePath)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(T.scenePath);
        if (!AssetDatabase.CopyAsset(SourceScenePath, T.scenePath))
        {
            Debug.LogError("Failed to copy source scene.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(T.scenePath, OpenSceneMode.Single);

        RemoveOldUI();
        SlotPrefabs prefabs = BuildSlotPrefabs();
        UIRefs ui = BuildUI();
        WireManagers(ui, prefabs);

        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = T.background;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{T.name} roulette scene built and saved at {T.scenePath}");
    }

    static string GenDir => string.IsNullOrEmpty(T.assetSub) ? GeneratedDir : $"{GeneratedDir}/{T.assetSub}";
    static string PrefDir => string.IsNullOrEmpty(T.assetSub) ? PrefabDir : $"{PrefabDir}/{T.assetSub}";

    // ---------- battle scene ----------

    const string BattleSourceScenePath = "Assets/Scenes/SampleScene.unity";
    const string BattleScenePath = "Assets/Scenes/TestBattleClean.unity";

    static void BuildBattleScene()
    {
        if (!File.Exists(BattleSourceScenePath))
        {
            Debug.LogError($"Source scene not found at {BattleSourceScenePath}");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        T = ArcaneTheme();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(T.fontPath);
        if (font == null) font = TMP_Settings.defaultFontAsset;

        GenerateSprites();

        if (SceneManager.GetActiveScene().path == BattleScenePath)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(BattleScenePath);
        if (!AssetDatabase.CopyAsset(BattleSourceScenePath, BattleScenePath))
        {
            Debug.LogError("Failed to copy battle source scene.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);

        RemoveOldUI();
        foreach (TurnBarView v in Object.FindObjectsOfType<TurnBarView>(true)) if (v != null) Object.DestroyImmediate(v.gameObject);
        foreach (HealthBarView v in Object.FindObjectsOfType<HealthBarView>(true)) if (v != null) Object.DestroyImmediate(v.gameObject);
        foreach (ResourceBarView v in Object.FindObjectsOfType<ResourceBarView>(true)) if (v != null) Object.DestroyImmediate(v.gameObject);

        BattleSimulator sim = Object.FindObjectOfType<BattleSimulator>(true);
        if (sim == null)
        {
            Debug.LogError("No BattleSimulator found in the battle scene.");
            return;
        }
        SeedCompositions(sim);

        CompleteItemGenerator.Generate();

        GameObject metaGO = new GameObject("BattleMetaManager");
        PlayerRoster roster = metaGO.AddComponent<PlayerRoster>();
        PlayerItemBank bankData = metaGO.AddComponent<PlayerItemBank>();
        bankData.capacity = 8;
        bankData.debugFillOnStart = true;
        bankData.recipeBook = AssetDatabase.LoadAssetAtPath<ItemRecipeBook>("Assets/Items/ItemRecipeBook.asset");
        SeedBankItems(bankData);
        CharacterEquipmentManager equip = metaGO.AddComponent<CharacterEquipmentManager>();
        equip.roster = roster;
        equip.bank = bankData;

        ItemSlotUI itemSlotPrefab = LoadOrBuildItemSlotPrefab();
        pipPrefab = BuildPipPrefab();

        sim.startOnAwake = false; // BattleIntroUI releases the fight after the enemy intro

        UIRefs ui = BuildBattleUI(sim);

        // Battle-scene controllers live on their own object, wired to the battle-local data
        GameObject controllers = new GameObject("UIControllers");
        ItemBankUIController bankUI = controllers.AddComponent<ItemBankUIController>();
        bankUI.bank = bankData;
        bankUI.equipmentManager = equip;
        bankUI.bankPanel = ui.bankPanel;
        bankUI.bankSlotsParent = ui.bankSlotsParent;
        bankUI.slotPrefab = itemSlotPrefab;
        bankUI.openBankButton = ui.openBankButton;
        bankUI.rootCanvas = ui.canvas;
        bankUI.dragGhostImage = ui.dragGhost;

        CharacterInfoPanelController info = controllers.AddComponent<CharacterInfoPanelController>();
        info.roster = roster;
        info.equipmentManager = equip;
        info.panelRoot = ui.infoPanelRoot;
        info.closeButton = ui.infoCloseButton;
        info.portraitImage = ui.infoPortrait;
        info.nameLabel = ui.infoName;
        info.levelLabel = ui.infoLevel;
        info.mementoLabel = ui.infoMemento;
        info.hpLabel = ui.hp;
        info.adLabel = ui.ad;
        info.apLabel = ui.ap;
        info.atkSpeedLabel = ui.atkSpd;
        info.critLabel = ui.crit;
        info.defLabel = ui.def;
        info.mdefLabel = ui.mdef;
        info.resourceLabel = ui.resource;
        info.frontlineAbilityRow = ui.frontAbilityRow;
        info.backlineAbilityRow = ui.backAbilityRow;
        info.equipmentSlots = ui.equipSlots;

        AbilityTooltip tooltip = controllers.AddComponent<AbilityTooltip>();
        tooltip.panelRoot = ui.tooltipRoot;
        tooltip.titleLabel = ui.tooltipTitle;
        tooltip.descriptionLabel = ui.tooltipDescription;

        sim.dialogueUI = ui.dialogue;

        BattlePauseController pauseCtrl = Object.FindObjectOfType<BattlePauseController>(true);
        if (pauseCtrl == null) pauseCtrl = sim.gameObject.AddComponent<BattlePauseController>();
        sim.pauseController = pauseCtrl;

        BattlePauseUI pauseUI = ui.canvas.gameObject.AddComponent<BattlePauseUI>();
        pauseUI.pauseController = pauseCtrl;
        pauseUI.pauseButton = ui.pauseButton;
        pauseUI.pauseButtonLabel = ui.pauseLabel;
        pauseUI.pausedBanner = ui.pausedBanner;
        pauseUI.resumeButton = ui.resumeButton;
        pauseUI.itemBankGroup = ui.bankGroup;
        pauseUI.messageLabel = ui.pauseMessage;

        BattleSpeedUI speedUI = ui.canvas.gameObject.AddComponent<BattleSpeedUI>();
        speedUI.speedButtons = ui.speedButtons;

        // Per the rules, equips/unequips in battle only happen during a pause
        foreach (EquippedItemSlotUI equipSlot in ui.equipSlots)
            equipSlot.pauseGate = pauseCtrl;

        BattleUIManager battleUIManager = Object.FindObjectOfType<BattleUIManager>(true);

        BattleClockUI clock = ui.canvas.gameObject.AddComponent<BattleClockUI>();
        clock.simulator = sim;
        clock.pauseController = pauseCtrl;
        clock.label = ui.clockLabel;

        BattleIntroUI introUI = ui.canvas.gameObject.AddComponent<BattleIntroUI>();
        introUI.simulator = sim;
        introUI.battleUI = battleUIManager;
        introUI.announceLabel = ui.announceLabel;
        introUI.focusFrame = ui.focusFrame;

        BattleResultUI resultUI = ui.canvas.gameObject.AddComponent<BattleResultUI>();
        resultUI.simulator = sim;
        resultUI.panelRoot = ui.resultPanel;
        resultUI.titleLabel = ui.resultTitle;
        resultUI.subtitleLabel = ui.resultSubtitle;
        resultUI.retryButton = ui.retryButton;

        BattleFxUI fx = ui.canvas.gameObject.AddComponent<BattleFxUI>();
        fx.simulator = sim;
        fx.battleUI = battleUIManager;
        fx.abilityPopupTemplate = ui.popupTemplate;
        fx.castRingSprite = raisedFrameSprite;

        GlobalItemGenerator.Generate();
        PlayerGlobalItemBank globalBank = metaGO.AddComponent<PlayerGlobalItemBank>();
        SeedGlobalItems(globalBank);

        GlobalItemManager globalManager = ui.canvas.gameObject.AddComponent<GlobalItemManager>();
        globalManager.simulator = sim;
        globalManager.pauseController = pauseCtrl;

        GlobalItemBankUI globalUI = ui.canvas.gameObject.AddComponent<GlobalItemBankUI>();
        globalUI.bank = globalBank;
        globalUI.manager = globalManager;
        globalUI.pauseController = pauseCtrl;
        globalUI.slots = ui.globalSlots;
        globalUI.panelGroup = ui.globalGroup;
        globalUI.messageLabel = ui.globalMessage;
        pauseUI.chargesLabel = ui.pauseChargesLabel;

        UIPanelToggle globalsToggle = ui.openGlobalsButton.gameObject.AddComponent<UIPanelToggle>();
        globalsToggle.button = ui.openGlobalsButton;
        globalsToggle.panel = ui.globalPanel;

        BattleAllyRosterSeeder seeder = metaGO.AddComponent<BattleAllyRosterSeeder>();
        seeder.simulator = sim;
        seeder.roster = roster;

        WireRecipePanel(ui);

        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = T.background;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EnsureInBuildSettings(BattleScenePath); // the RETRY button reloads the scene by name
        Debug.Log($"Clean battle scene built and saved at {BattleScenePath}");
    }

    static GameObject BuildPipPrefab()
    {
        RectTransform root = NewRect("ResourcePipClean", null);
        root.sizeDelta = new Vector2(12, 12);
        AddImage(root, T.accent, circleSprite, false);
        Outline outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1, -1);
        return SavePrefab(root.gameObject, $"{PrefDir}/ResourcePipClean.prefab");
    }

    static void EnsureInBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == scenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // Only seeds when every slot is empty, so a hand-configured composition is never clobbered
    static void SeedCompositions(BattleSimulator sim)
    {
        bool HasAny(System.Collections.Generic.List<SlotAssignment> list)
        {
            foreach (SlotAssignment a in list) if (a.character != null) return true;
            return false;
        }
        if (HasAny(sim.allyComposition) || HasAny(sim.enemyComposition)) return;

        string[] guids = AssetDatabase.FindAssets("t:CharacterData");
        var champions = new System.Collections.Generic.List<CharacterData>();
        foreach (string guid in guids)
        {
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null) champions.Add(data);
        }
        if (champions.Count == 0)
        {
            Debug.LogWarning("No CharacterData assets found - battle compositions left empty.");
            return;
        }

        void Fill(System.Collections.Generic.List<SlotAssignment> list, BattlePosition position, int count)
        {
            for (int i = 0; i < count; i++)
                list.Add(new SlotAssignment
                {
                    character = champions[Random.Range(0, champions.Count)],
                    position = position,
                    slotIndex = i,
                    level = 1
                });
        }

        sim.allyComposition.Clear();
        sim.enemyComposition.Clear();
        Fill(sim.allyComposition, BattlePosition.Frontline, sim.allyFrontlineSlots);
        Fill(sim.allyComposition, BattlePosition.Backline, sim.allyBacklineSlots);
        Fill(sim.enemyComposition, BattlePosition.Frontline, sim.enemyFrontlineSlots);
        Fill(sim.enemyComposition, BattlePosition.Backline, sim.enemyBacklineSlots);
    }

    // Includes a duplicate so stacking is visible right away
    static void SeedGlobalItems(PlayerGlobalItemBank globalBank)
    {
        globalBank.capacity = 8;
        globalBank.debugFillOnStart = true;
        globalBank.debugSeedItems.Clear();
        string[] seedNames =
        {
            "Ruthanian Anarchy Protocol", "Ruthanian Anarchy Protocol",
            "Halcyon Dream", "The Necromancer's Call", "Conviction"
        };
        foreach (string itemName in seedNames)
        {
            GlobalItemData item = AssetDatabase.LoadAssetAtPath<GlobalItemData>($"Assets/Items/Globals/{itemName}.asset");
            if (item != null) globalBank.debugSeedItems.Add(item);
        }
    }

    // Components chosen so recipes are testable out of the box:
    // Long Sword + Sparring Gloves = Finésse, Swift Knife + Sparring Gloves = Karkrinos,
    // Battle Axe + Battle Axe = Bloodletter
    static void SeedBankItems(PlayerItemBank bankData)
    {
        string[] names = { "Long Sword", "Sparring Gloves", "Swift Knife", "Sparring Gloves", "Battle Axe", "Battle Axe" };
        foreach (string componentName in names)
        {
            ItemComponentData item = AssetDatabase.LoadAssetAtPath<ItemComponentData>($"Assets/Items/Components/{componentName}.asset");
            if (item != null) bankData.debugSeedItems.Add(item);
        }
    }

    // Reuses the roulette scene's prefab when it exists so both scenes share one asset;
    // rebuilds when the stored prefab predates the combine bar
    static ItemSlotUI LoadOrBuildItemSlotPrefab()
    {
        GameObject prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefDir}/ItemSlotClean.prefab");
        ItemSlotUI slot = prefabGO != null ? prefabGO.GetComponent<ItemSlotUI>() : null;
        return slot != null && slot.combineBar != null ? slot : BuildSlotPrefabs().itemSlot;
    }

    static UIRefs BuildBattleUI(BattleSimulator sim)
    {
        UIRefs ui = new UIRefs();

        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;
        ui.canvas = canvasGO.GetComponent<Canvas>();
        ui.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGO.transform;

        RectTransform bg = NewRect("Background", canvas);
        StretchInset(bg, 0);
        AddImage(bg, Color.white, gradientSprite, false);

        ArenaGlow(canvas, "ArenaGlowCenter", new Vector2(0, -40), new Vector2(1100, 620), T.accent, 0.05f);
        ArenaGlow(canvas, "ArenaGlowLeft", new Vector2(-575, -40), new Vector2(700, 520), Hex("C06078"), 0.05f);
        ArenaGlow(canvas, "ArenaGlowRight", new Vector2(575, -40), new Vector2(700, 520), T.framePrimary, 0.05f);

        var (bar, _, _) = Window(canvas, "TopBar");
        bar.anchorMin = new Vector2(0, 1);
        bar.anchorMax = new Vector2(1, 1);
        bar.pivot = new Vector2(0.5f, 1);
        bar.offsetMin = new Vector2(10, -64);
        bar.offsetMax = new Vector2(-10, -8);
        ui.openBankButton = TopBarButton(bar, "EquipButton", "EQUIP", -20);
        ui.openGlobalsButton = TopBarButton(bar, "GlobalsButton", "GLOBALS", -124, 110);

        ui.clockLabel = NewText(bar, "ClockLabel", "0:00", 28, T.accentBright, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform clockRt = (RectTransform)ui.clockLabel.transform;
        clockRt.anchorMin = new Vector2(0.5f, 0.5f);
        clockRt.anchorMax = new Vector2(0.5f, 0.5f);
        clockRt.pivot = new Vector2(0.5f, 0.5f);
        clockRt.anchoredPosition = Vector2.zero;
        clockRt.sizeDelta = new Vector2(200, 44);
        ui.clockLabel.characterSpacing = 2;

        // Floats just under the top bar so it never fights the centered clock
        ui.announceLabel = NewText(canvas, "AnnounceLabel", "", 22, T.accentBright, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform announceRt = (RectTransform)ui.announceLabel.transform;
        announceRt.anchorMin = new Vector2(0.5f, 1);
        announceRt.anchorMax = new Vector2(0.5f, 1);
        announceRt.pivot = new Vector2(0.5f, 1);
        announceRt.anchoredPosition = new Vector2(0, -134);
        announceRt.sizeDelta = new Vector2(640, 40);
        ui.announceLabel.characterSpacing = 4;
        ui.announceLabel.enableWordWrapping = false;
        ui.announceLabel.overflowMode = TextOverflowModes.Overflow;

        // Shared red warning line for charge-denied actions
        ui.pauseMessage = NewText(canvas, "PauseMessageLabel", "", 15, Hex("F87060"), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        RectTransform pauseMsgRt = (RectTransform)ui.pauseMessage.transform;
        pauseMsgRt.anchorMin = new Vector2(0, 1);
        pauseMsgRt.anchorMax = new Vector2(0, 1);
        pauseMsgRt.pivot = new Vector2(0, 1);
        pauseMsgRt.anchoredPosition = new Vector2(20, -76);
        pauseMsgRt.sizeDelta = new Vector2(520, 28);

        // Speed/pause control strip under the centered clock: 5 speed buttons, the pause
        // button and the pause-charge counter
        var (speedBar, _, _) = Window(canvas, "SpeedBar");
        speedBar.anchorMin = new Vector2(0.5f, 1);
        speedBar.anchorMax = new Vector2(0.5f, 1);
        speedBar.pivot = new Vector2(0.5f, 1);
        speedBar.anchoredPosition = new Vector2(0, -70);
        speedBar.sizeDelta = new Vector2(390, 54);

        ui.speedButtons = new Button[5];
        float sx = 12f;
        for (int i = 0; i < 5; i++)
        {
            ui.speedButtons[i] = IconButton(speedBar, $"SpeedButton{i}", speedIconSprites[i], new Vector2(sx, -7), new Vector2(44, 40),
                Colors(T.frameSecondary, T.accentBright, T.accentPressed, T.accent)); // disabled = selected = accent
            sx += 50f;
        }

        sx += 6f;
        ui.pauseButton = IconButton(speedBar, "PauseButton", pauseIconSprite, new Vector2(sx, -7), new Vector2(44, 40),
            Colors(T.frameSecondary, T.accentBright, T.accentPressed, T.frameDisabled));
        sx += 52f;

        var (chargesChip, _, _) = Box(speedBar, "PauseChargesChip", T.slotFill, T.frameSecondary, false);
        SetTopLeft(chargesChip, new Vector2(sx, -7), new Vector2(58, 40));
        ui.pauseChargesLabel = NewText(chargesChip, "Count", "0", 22, T.accentBright, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)ui.pauseChargesLabel.transform, 0);

        var (bank, _, _) = Window(canvas, "BankPanel");
        SetTopRight(bank, new Vector2(-10, -72), new Vector2(440, 272));
        ui.bankPanel = bank.gameObject;
        ui.bankGroup = bank.gameObject.AddComponent<CanvasGroup>();
        SectionTitle(bank, "EQUIPMENT", -16);
        ui.bankSlotsParent = SlotGrid(bank, "BankSlots", -58, 92, 10, 4, 398, 194);

        // Enemies on the left, the player's squad on the right; frontlines face each other
        BattleUIManager battleUI = Object.FindObjectOfType<BattleUIManager>(true);
        if (battleUI != null)
        {
            battleUI.enemyBacklineSlots = BuildSlotColumn(canvas, "EnemyBack", sim.enemyBacklineSlots, -680, true);
            battleUI.enemyFrontlineSlots = BuildSlotColumn(canvas, "EnemyFront", sim.enemyFrontlineSlots, -470, true);
            battleUI.allyFrontlineSlots = BuildSlotColumn(canvas, "AllyFront", sim.allyFrontlineSlots, 470, false);
            battleUI.allyBacklineSlots = BuildSlotColumn(canvas, "AllyBack", sim.allyBacklineSlots, 680, false);
        }

        // Built after the unit columns so it overlays the enemy cards when opened;
        // starts closed so the intro focus stays visible
        BuildGlobalItemsPanel(canvas, ui, new Vector2(10, -72), false);

        BuildPausedBanner(canvas, ui);

        // Intro focus frame - moved and blinked over enemy cards by BattleIntroUI
        RectTransform focus = NewRect("IntroFocusFrame", canvas);
        focus.sizeDelta = new Vector2(152, 194);
        AddImage(focus, T.accentBright, raisedFrameSprite, false);
        focus.gameObject.SetActive(false);
        ui.focusFrame = focus;

        // Ability-name popup template, instantiated by BattleFxUI
        var (popup, _, _) = Box(canvas, "AbilityPopupTemplate", Hex("0B0716"), T.accent, false);
        popup.sizeDelta = new Vector2(180, 38);
        TMP_Text popupLabel = NewText(popup, "Label", "Ability", 14, T.textPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)popupLabel.transform, 0);
        popupLabel.enableWordWrapping = false;
        popupLabel.overflowMode = TextOverflowModes.Overflow;
        popup.gameObject.SetActive(false);
        ui.popupTemplate = popup;

        BuildResultPanel(canvas, ui);

        BuildCharacterInfoPanel(canvas, ui);
        BuildRecipePanel(canvas, ui);
        BuildDialogue(canvas, ui);
        BuildTooltip(canvas, ui);

        RectTransform ghost = NewRect("DragGhost", canvas);
        ghost.sizeDelta = new Vector2(110, 110);
        ui.dragGhost = AddImage(ghost, Color.white, null, false);
        ui.dragGhost.enabled = false;

        return ui;
    }

    static void BuildOtherItemsPanel(Transform canvas, UIRefs ui, Vector2 topLeftPos)
    {
        var (panel, panelFill, _) = Window(canvas, "OtherItemsPanel");
        SetTopLeft(panel, topLeftPos, new Vector2(300, 168));
        panelFill.raycastTarget = true;
        ui.otherPanel = panel.gameObject;
        SectionTitle(panel, "MAP ITEMS", -16);

        ui.otherSlots = new OtherItemSlotUI[4];
        for (int i = 0; i < 4; i++)
            ui.otherSlots[i] = OtherItemSlot(panel, $"OtherSlot{i + 1}", new Vector2(20 + i * 68, -52));

        panel.gameObject.SetActive(false);
    }

    static OtherItemSlotUI OtherItemSlot(RectTransform panel, string name, Vector2 pos)
    {
        var (root, _, _) = Box(panel, name, T.slotFill, T.frameSecondary, true);
        SetTopLeft(root, pos, new Vector2(60, 60));

        RectTransform icon = NewRect("Icon", root);
        StretchInset(icon, 9);
        Image iconImg = AddImage(icon, Color.white, null, false);
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        TMP_Text fallback = NewText(root, "FallbackLabel", "", 10, T.textPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)fallback.transform, 5);

        TMP_Text count = NewText(root, "CountLabel", "", 13, T.accentBright, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        RectTransform countRt = (RectTransform)count.transform;
        countRt.anchorMin = new Vector2(1, 0);
        countRt.anchorMax = new Vector2(1, 0);
        countRt.pivot = new Vector2(1, 0);
        countRt.anchoredPosition = new Vector2(-6, 5);
        countRt.sizeDelta = new Vector2(36, 18);

        OtherItemSlotUI slot = root.gameObject.AddComponent<OtherItemSlotUI>();
        slot.iconImage = iconImg;
        slot.fallbackNameLabel = fallback;
        slot.countLabel = count;
        return slot;
    }

    static void BuildArtisanPanel(Transform canvas, UIRefs ui)
    {
        RectTransform overlay = NewRect("ArtisanPanel", canvas);
        StretchInset(overlay, 0);
        AddImage(overlay, new Color(0f, 0f, 0.01f, 0.7f), null, true); // dim + block clicks behind
        ui.artisanPanelRoot = overlay.gameObject;

        var (win, winFill, _) = Window(overlay, "Window");
        win.anchorMin = new Vector2(0.5f, 0.5f);
        win.anchorMax = new Vector2(0.5f, 0.5f);
        win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(700, 260);
        winFill.raycastTarget = true;

        TMP_Text title = NewText(win, "Title", "ARTISAN'S ASPECT - CHOOSE ONE", 18, T.accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)title.transform, new Vector2(24, -16), new Vector2(560, 28));
        title.characterSpacing = 2;

        ui.artisanCloseButton = NewButton(win, "CloseButton", "X", new Vector2(34, 34), 16, T.textPrimary, StandardFrameColors());
        RectTransform closeRt = (RectTransform)ui.artisanCloseButton.transform;
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.anchoredPosition = new Vector2(-14, -14);

        ui.artisanChoices = new ArtisanAspectPanelUI.Choice[5];
        for (int i = 0; i < 5; i++)
            ui.artisanChoices[i] = ArtisanChoice(win, $"Choice{i + 1}", new Vector2(24 + i * 132, -64));

        overlay.gameObject.SetActive(false);
    }

    static ArtisanAspectPanelUI.Choice ArtisanChoice(RectTransform win, string name, Vector2 pos)
    {
        var (root, _, frame) = Box(win, name, T.slotFill, T.frameSecondary, true);
        SetTopLeft(root, pos, new Vector2(120, 150));
        frame.color = Color.white;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = frame;
        button.colors = Colors(T.frameSecondary, T.accentBright, T.accentPressed, T.frameSecondary);

        RectTransform iconBox = NewRect("IconBox", root);
        SetTopLeft(iconBox, new Vector2(30, -14), new Vector2(60, 60));
        Image iconImg = AddImage(iconBox, Color.white, null, false);
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        TMP_Text nameLabel = NewText(root, "Name", "", 12, T.textPrimary, TextAlignmentOptions.Top, FontStyles.Bold);
        RectTransform nameRt = (RectTransform)nameLabel.transform;
        SetTopLeft(nameRt, new Vector2(6, -80), new Vector2(108, 62));

        ItemTooltipTrigger tooltip = root.gameObject.AddComponent<ItemTooltipTrigger>();

        return new ArtisanAspectPanelUI.Choice
        {
            root = root.gameObject,
            button = button,
            icon = iconImg,
            nameLabel = nameLabel,
            tooltip = tooltip
        };
    }

    static void BuildRecipePanel(Transform canvas, UIRefs ui)
    {
        var (win, winFill, _) = Window(canvas, "RecipePanel");
        win.anchorMin = new Vector2(0.5f, 0.5f);
        win.anchorMax = new Vector2(0.5f, 0.5f);
        win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(620, 596);
        winFill.raycastTarget = true;
        ui.recipePanelRoot = win.gameObject;

        ui.recipeTitle = NewText(win, "Title", "Recipes", 18, T.accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)ui.recipeTitle.transform, new Vector2(24, -16), new Vector2(480, 28));
        ui.recipeTitle.characterSpacing = 2;
        ui.recipeTitle.enableWordWrapping = false;
        ui.recipeTitle.overflowMode = TextOverflowModes.Ellipsis;

        ui.recipeCloseButton = NewButton(win, "CloseButton", "X", new Vector2(34, 34), 16, T.textPrimary, StandardFrameColors());
        RectTransform closeRt = (RectTransform)ui.recipeCloseButton.transform;
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.anchoredPosition = new Vector2(-14, -14);

        ui.recipeRows = new ItemRecipeRowUI[10];
        for (int i = 0; i < ui.recipeRows.Length; i++)
            ui.recipeRows[i] = RecipeRow(win, $"RecipeRow{i + 1}", -58 - i * 52);

        ui.recipeEmptyLabel = NewText(win, "EmptyLabel", "No recipes use this item.", 15, T.textSecondary, TextAlignmentOptions.Center, FontStyles.Normal);
        RectTransform emptyRt = (RectTransform)ui.recipeEmptyLabel.transform;
        emptyRt.anchorMin = new Vector2(0, 1);
        emptyRt.anchorMax = new Vector2(1, 1);
        emptyRt.pivot = new Vector2(0.5f, 1);
        emptyRt.anchoredPosition = new Vector2(0, -90);
        emptyRt.sizeDelta = new Vector2(0, 30);
        ui.recipeEmptyLabel.gameObject.SetActive(false);
    }

    static ItemRecipeRowUI RecipeRow(RectTransform win, string name, float y)
    {
        RectTransform row = NewRect(name, win);
        SetTopLeft(row, new Vector2(24, y), new Vector2(572, 46));
        ItemRecipeRowUI rowUI = row.gameObject.AddComponent<ItemRecipeRowUI>();

        (rowUI.iconA, rowUI.tooltipA) = RecipeIcon(row, "IconA", 0);
        TMP_Text plus = NewText(row, "Plus", "+", 18, T.textSecondary, TextAlignmentOptions.Center, FontStyles.Bold);
        SetTopLeft((RectTransform)plus.transform, new Vector2(44, -8), new Vector2(20, 30));
        (rowUI.iconB, rowUI.tooltipB) = RecipeIcon(row, "IconB", 66);
        TMP_Text equals = NewText(row, "Equals", "=", 18, T.accent, TextAlignmentOptions.Center, FontStyles.Bold);
        SetTopLeft((RectTransform)equals.transform, new Vector2(110, -8), new Vector2(20, 30));
        (rowUI.iconResult, rowUI.tooltipResult) = RecipeIcon(row, "IconResult", 132);

        rowUI.resultNameLabel = NewText(row, "ResultName", "", 15, T.textPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        RectTransform nameRt = (RectTransform)rowUI.resultNameLabel.transform;
        SetTopLeft(nameRt, new Vector2(184, -8), new Vector2(380, 30));
        rowUI.resultNameLabel.enableWordWrapping = false;
        rowUI.resultNameLabel.overflowMode = TextOverflowModes.Ellipsis;

        return rowUI;
    }

    static (Image icon, ItemTooltipTrigger tooltip) RecipeIcon(RectTransform row, string name, float x)
    {
        var (box, _, _) = Box(row, name, T.slotFill, T.frameSecondary, true);
        SetTopLeft(box, new Vector2(x, -3), new Vector2(40, 40));
        RectTransform icon = NewRect("Icon", box);
        StretchInset(icon, 5);
        Image img = AddImage(icon, Color.white, null, false);
        img.preserveAspect = true;
        img.enabled = false;
        ItemTooltipTrigger trigger = box.gameObject.AddComponent<ItemTooltipTrigger>();
        return (img, trigger);
    }

    static void WireRecipePanel(UIRefs ui)
    {
        ItemRecipePanelUI recipeUI = ui.canvas.gameObject.AddComponent<ItemRecipePanelUI>();
        recipeUI.recipeBook = AssetDatabase.LoadAssetAtPath<ItemRecipeBook>("Assets/Items/ItemRecipeBook.asset");
        recipeUI.panelRoot = ui.recipePanelRoot;
        recipeUI.titleLabel = ui.recipeTitle;
        recipeUI.emptyLabel = ui.recipeEmptyLabel;
        recipeUI.closeButton = ui.recipeCloseButton;
        recipeUI.rows = ui.recipeRows;
    }

    static Button IconButton(RectTransform parent, string name, Sprite iconSprite, Vector2 pos, Vector2 size, ColorBlock colors)
    {
        var (root, _, frame) = Window(parent, name);
        SetTopLeft(root, pos, size);
        frame.raycastTarget = true;
        frame.color = Color.white; // frame tint comes entirely from the ColorBlock

        Button btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = frame;
        btn.colors = colors;

        RectTransform icon = NewRect("Icon", root);
        StretchInset(icon, 11);
        Image img = AddImage(icon, T.textPrimary, iconSprite, false);
        img.preserveAspect = true;
        return btn;
    }

    static void BuildGlobalItemsPanel(Transform canvas, UIRefs ui, Vector2 topLeftPos, bool startOpen)
    {
        var (globals, globalsFill, _) = Window(canvas, "GlobalItemsPanel");
        SetTopLeft(globals, topLeftPos, new Vector2(440, 300));
        globalsFill.raycastTarget = true; // blocks clicks to whatever sits behind when open
        ui.globalPanel = globals.gameObject;
        ui.globalGroup = globals.gameObject.AddComponent<CanvasGroup>();
        SectionTitle(globals, "GLOBAL ITEMS", -16);

        ui.globalSlots = new GlobalItemSlotUI[8];
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            ui.globalSlots[i] = GlobalItemSlot(globals, $"GlobalSlot{i + 1}", new Vector2(20 + col * 102, -52 - row * 104));
        }

        ui.globalMessage = NewText(globals, "MessageLabel", "", 14, Hex("F87060"), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)ui.globalMessage.transform, new Vector2(20, -262), new Vector2(404, 28));

        globals.gameObject.SetActive(startOpen);
    }

    static GlobalItemSlotUI GlobalItemSlot(RectTransform panel, string name, Vector2 pos)
    {
        var (root, _, frame) = Box(panel, name, T.slotFill, T.frameSecondary, true);
        SetTopLeft(root, pos, new Vector2(96, 100));
        frame.color = Color.white; // tinted by the button's ColorBlock

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = frame;
        button.colors = Colors(T.frameSecondary, T.accentBright, T.accentPressed, new Color(0.32f, 0.32f, 0.38f, 1f));

        RectTransform icon = NewRect("Icon", root);
        StretchInset(icon, 16);
        Image iconImg = AddImage(icon, Color.white, null, false);
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        TMP_Text fallback = NewText(root, "FallbackLabel", "", 11, T.textPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)fallback.transform, 8);

        TMP_Text count = NewText(root, "CountLabel", "", 14, T.accentBright, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        RectTransform countRt = (RectTransform)count.transform;
        countRt.anchorMin = new Vector2(1, 0);
        countRt.anchorMax = new Vector2(1, 0);
        countRt.pivot = new Vector2(1, 0);
        countRt.anchoredPosition = new Vector2(-9, 8);
        countRt.sizeDelta = new Vector2(44, 20);

        GlobalItemSlotUI slot = root.gameObject.AddComponent<GlobalItemSlotUI>();
        slot.button = button;
        slot.iconImage = iconImg;
        slot.fallbackNameLabel = fallback;
        slot.countLabel = count;
        return slot;
    }

    static void BuildPausedBanner(Transform canvas, UIRefs ui)
    {
        var (banner, _, _) = Window(canvas, "PausedBanner");
        banner.anchorMin = new Vector2(0.5f, 1);
        banner.anchorMax = new Vector2(0.5f, 1);
        banner.pivot = new Vector2(0.5f, 1);
        banner.anchoredPosition = new Vector2(0, -140);
        banner.sizeDelta = new Vector2(560, 180);

        TMP_Text title = NewText(banner, "Title", "PAUSED", 30, T.accent, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -18);
        titleRt.sizeDelta = new Vector2(0, 38);
        title.characterSpacing = 6;

        TMP_Text subtitle = NewText(banner, "Subtitle", "Equip gear or use global items - each action costs 1 charge", 15, T.textSecondary, TextAlignmentOptions.Center, FontStyles.Normal);
        RectTransform subRt = (RectTransform)subtitle.transform;
        subRt.anchorMin = new Vector2(0, 1);
        subRt.anchorMax = new Vector2(1, 1);
        subRt.pivot = new Vector2(0.5f, 1);
        subRt.anchoredPosition = new Vector2(0, -60);
        subRt.sizeDelta = new Vector2(0, 24);

        ui.resumeButton = NewButton(banner, "ResumeButton", "RESUME", new Vector2(180, 48), 16, T.textPrimary, StandardFrameColors());
        RectTransform resumeRt = (RectTransform)ui.resumeButton.transform;
        resumeRt.anchorMin = new Vector2(0.5f, 0);
        resumeRt.anchorMax = new Vector2(0.5f, 0);
        resumeRt.pivot = new Vector2(0.5f, 0);
        resumeRt.anchoredPosition = new Vector2(0, 18);

        banner.gameObject.SetActive(false);
        ui.pausedBanner = banner.gameObject;
    }

    static CharacterSlotUI[] BuildSlotColumn(Transform canvas, string prefix, int count, float x, bool enemy)
    {
        const float rowSpacing = 235f;
        CharacterSlotUI[] slots = new CharacterSlotUI[count];
        for (int i = 0; i < count; i++)
        {
            float y = ((count - 1) * 0.5f - i) * rowSpacing - 30f;
            slots[i] = BuildBattleSlot(canvas, $"{prefix}Slot{i}", new Vector2(x, y), enemy);
        }
        return slots;
    }

    // One unit "card": gradient backplate with a side-tinted beveled frame, portrait,
    // three bars and a name strip
    static CharacterSlotUI BuildBattleSlot(Transform canvas, string name, Vector2 pos, bool enemy)
    {
        RectTransform root = NewRect(name, canvas);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = pos;
        root.sizeDelta = new Vector2(130, 172);

        RectTransform cardFill = NewRect("Fill", root);
        StretchInset(cardFill, 4);
        AddImage(cardFill, Color.white, fillSprite, false);
        RectTransform cardFrame = NewRect("Frame", root);
        StretchInset(cardFrame, 0);
        AddImage(cardFrame, enemy ? Hex("C06078") : T.framePrimary, raisedFrameSprite, true);

        var (portraitBox, _, _) = Box(root, "PortraitBox", T.slotFill, T.frameSecondary, false);
        portraitBox.anchorMin = new Vector2(0.5f, 1);
        portraitBox.anchorMax = new Vector2(0.5f, 1);
        portraitBox.pivot = new Vector2(0.5f, 1);
        portraitBox.anchoredPosition = new Vector2(0, -8);
        portraitBox.sizeDelta = new Vector2(106, 106);
        RectTransform portrait = NewRect("Portrait", portraitBox); StretchInset(portrait, 7);
        Image portraitImg = AddImage(portrait, new Color(1, 1, 1, 0), null, false);

        Color hpColor = enemy ? Hex("E85868") : Hex("58E878");
        Image turnFill = BarVisual(root, "TurnBar", -117, 6, 106, Hex("E8D858"), out RectTransform turnRt);
        TurnBarView turnBar = turnRt.gameObject.AddComponent<TurnBarView>();
        turnBar.fillImage = turnFill;

        Image healthFill = BarVisual(root, "HealthBar", -125, 12, 106, hpColor, out RectTransform healthRt);
        HealthBarView healthBar = healthRt.gameObject.AddComponent<HealthBarView>();
        healthBar.fillImage = healthFill;

        // hover target + in-bar "current/max" readout
        healthRt.GetComponent<Image>().raycastTarget = true;
        TMP_Text hpValue = NewText(healthRt, "ValueLabel", "", 11, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)hpValue.transform, 0);
        hpValue.enableWordWrapping = false;
        hpValue.overflowMode = TextOverflowModes.Overflow;
        healthBar.valueLabel = hpValue;

        Image resourceFill = BarVisual(root, "ResourceBar", -139, 10, 106, T.accent, out RectTransform resourceRt);
        ResourceBarView resourceBar = resourceRt.gameObject.AddComponent<ResourceBarView>();
        resourceBar.fillImage = resourceFill;

        RectTransform stackContainer = NewRect("StackContainer", resourceRt);
        StretchInset(stackContainer, 0);
        HorizontalLayoutGroup stackLayout = stackContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        stackLayout.spacing = 3;
        stackLayout.padding = new RectOffset(2, 2, 0, 0);
        stackLayout.childAlignment = TextAnchor.MiddleLeft;
        stackLayout.childControlWidth = false;
        stackLayout.childControlHeight = false;
        stackLayout.childForceExpandWidth = false;
        stackLayout.childForceExpandHeight = false;

        TMP_Text stackLabel = NewText(resourceRt, "StackCount", "", 13, T.textPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform stackRt = (RectTransform)stackLabel.transform;
        stackRt.anchorMin = new Vector2(0.5f, 0.5f);
        stackRt.anchorMax = new Vector2(0.5f, 0.5f);
        stackRt.pivot = new Vector2(0.5f, 0.5f);
        stackRt.anchoredPosition = Vector2.zero;
        stackRt.sizeDelta = new Vector2(106, 20);
        stackLabel.enableWordWrapping = false;
        stackLabel.overflowMode = TextOverflowModes.Overflow;

        resourceBar.stackContainer = stackContainer;
        resourceBar.stackPipPrefab = pipPrefab != null ? pipPrefab : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ResourcePip.prefab");
        resourceBar.stackCountLabel = stackLabel;

        RectTransform nameBand = NewRect("NameBand", root);
        nameBand.anchorMin = new Vector2(0.5f, 1);
        nameBand.anchorMax = new Vector2(0.5f, 1);
        nameBand.pivot = new Vector2(0.5f, 1);
        nameBand.anchoredPosition = new Vector2(0, -151);
        nameBand.sizeDelta = new Vector2(106, 16);
        AddImage(nameBand, new Color(0f, 0f, 0.02f, 0.5f), null, false);
        TMP_Text nameLabel = NewText(nameBand, "Label", "", 12, T.textPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)nameLabel.transform, 0);
        nameLabel.enableWordWrapping = false;
        nameLabel.overflowMode = TextOverflowModes.Overflow;

        BattleUnitView view = root.gameObject.AddComponent<BattleUnitView>();
        view.portraitImage = portraitImg;
        view.nameLabel = nameLabel;

        return new CharacterSlotUI { turnBar = turnBar, healthBar = healthBar, resourceBar = resourceBar, unitView = view };
    }

    static Image BarVisual(Transform parent, string name, float y, float height, float width, Color fillColor, out RectTransform barRt)
    {
        barRt = NewRect(name, parent);
        barRt.anchorMin = new Vector2(0.5f, 1);
        barRt.anchorMax = new Vector2(0.5f, 1);
        barRt.pivot = new Vector2(0.5f, 1);
        barRt.anchoredPosition = new Vector2(0, y);
        barRt.sizeDelta = new Vector2(width, height);

        AddImage(barRt, Hex("050508"), null, false);
        Outline outline = barRt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1, -1);

        RectTransform fillRt = NewRect("Fill", barRt);
        StretchInset(fillRt, 1);
        Image fill = AddImage(fillRt, fillColor, barFillSprite, false);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        return fill;
    }

    static void ArenaGlow(Transform canvas, string name, Vector2 pos, Vector2 size, Color color, float alpha)
    {
        RectTransform glow = NewRect(name, canvas);
        glow.anchorMin = new Vector2(0.5f, 0.5f);
        glow.anchorMax = new Vector2(0.5f, 0.5f);
        glow.pivot = new Vector2(0.5f, 0.5f);
        glow.anchoredPosition = pos;
        glow.sizeDelta = size;
        AddImage(glow, new Color(color.r, color.g, color.b, alpha), radialSprite, false);
    }

    static void BuildResultPanel(Transform canvas, UIRefs ui)
    {
        RectTransform overlay = NewRect("ResultPanel", canvas);
        StretchInset(overlay, 0);
        AddImage(overlay, new Color(0f, 0f, 0.01f, 0.78f), null, true);

        var (win, _, _) = Window(overlay, "Window");
        win.anchorMin = new Vector2(0.5f, 0.5f);
        win.anchorMax = new Vector2(0.5f, 0.5f);
        win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(620, 320);

        ui.resultTitle = NewText(win, "Title", "VICTORY", 54, T.accent, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform titleRt = (RectTransform)ui.resultTitle.transform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -42);
        titleRt.sizeDelta = new Vector2(0, 70);
        ui.resultTitle.characterSpacing = 8;

        ui.resultSubtitle = NewText(win, "Subtitle", "", 17, T.textSecondary, TextAlignmentOptions.Center, FontStyles.Normal);
        RectTransform subRt = (RectTransform)ui.resultSubtitle.transform;
        subRt.anchorMin = new Vector2(0, 1);
        subRt.anchorMax = new Vector2(1, 1);
        subRt.pivot = new Vector2(0.5f, 1);
        subRt.anchoredPosition = new Vector2(0, -128);
        subRt.sizeDelta = new Vector2(0, 30);

        ui.retryButton = NewButton(win, "RetryButton", "RETRY", new Vector2(200, 54), 17, T.textPrimary, StandardFrameColors());
        RectTransform retryRt = (RectTransform)ui.retryButton.transform;
        retryRt.anchorMin = new Vector2(0.5f, 0);
        retryRt.anchorMax = new Vector2(0.5f, 0);
        retryRt.pivot = new Vector2(0.5f, 0);
        retryRt.anchoredPosition = new Vector2(0, 30);

        overlay.gameObject.SetActive(false);
        ui.resultPanel = overlay.gameObject;
    }

    // ---------- old UI removal ----------

    static void RemoveOldUI()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c != null && c.transform.parent == null)
                Object.DestroyImmediate(c.gameObject);

        foreach (DialogueUIController d in Object.FindObjectsOfType<DialogueUIController>(true))
        {
            if (d == null) continue;
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(d.gameObject);
            Object.DestroyImmediate(root != null ? root : d.gameObject);
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetAsLastSibling();
        }
    }

    // ---------- sprite generation ----------

    static void GenerateSprites()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, GenDir.Substring("Assets/".Length)));
        AssetDatabase.Refresh();

        raisedFrameSprite = MakeSprite($"{GenDir}/frame_raised.png", BevelFrameTexture(48, true), new Vector4(14, 14, 14, 14));
        insetFrameSprite = MakeSprite($"{GenDir}/frame_inset.png", BevelFrameTexture(48, false), new Vector4(14, 14, 14, 14));
        fillSprite = MakeSprite($"{GenDir}/window_fill.png", WindowFillTexture(8, 64), Vector4.zero);
        circleSprite = MakeSprite($"{GenDir}/circle.png", CircleTexture(64), Vector4.zero);
        gradientSprite = MakeSprite($"{GenDir}/bg_gradient.png", GradientTexture(4, 256, T.bgTop, T.bgBottom), Vector4.zero);
        radialSprite = MakeSprite($"{GenDir}/radial_glow.png", RadialGlowTexture(128), Vector4.zero);
        barFillSprite = MakeSprite($"{GenDir}/bar_fill.png", GradientTexture(4, 16, Color.white, new Color(0.55f, 0.55f, 0.55f)), Vector4.zero);

        speedIconSprites = new Sprite[5];
        speedIconSprites[0] = MakeSprite($"{GenDir}/icon_speed0.png", SpeedIconTexture(32, 2, true), Vector4.zero);
        speedIconSprites[1] = MakeSprite($"{GenDir}/icon_speed1.png", SpeedIconTexture(32, 1, true), Vector4.zero);
        speedIconSprites[2] = MakeSprite($"{GenDir}/icon_speed2.png", SpeedIconTexture(32, 1, false), Vector4.zero);
        speedIconSprites[3] = MakeSprite($"{GenDir}/icon_speed3.png", SpeedIconTexture(32, 2, false), Vector4.zero);
        speedIconSprites[4] = MakeSprite($"{GenDir}/icon_speed4.png", SpeedIconTexture(32, 3, false), Vector4.zero);
        pauseIconSprite = MakeSprite($"{GenDir}/icon_pause.png", PauseIconTexture(32), Vector4.zero);
    }

    // Side-by-side play-style triangles: left-pointing for slow speeds, right for fast
    static Texture2D SpeedIconTexture(int size, int triangleCount, bool pointLeft)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float margin = 6f;
        float contentWidth = size - margin * 2f;
        float triangleWidth = contentWidth / triangleCount;
        float halfHeight = (size - margin * 2f) * 0.5f;
        float centerY = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x + 0.5f - margin;
            float py = Mathf.Abs(y + 0.5f - centerY);
            float alpha = 0f;

            if (px >= 0f && px < contentWidth)
            {
                float dx = px % triangleWidth;
                float t = dx / triangleWidth;
                float rowHalf = (pointLeft ? t : 1f - t) * halfHeight;
                alpha = Mathf.Clamp01(rowHalf - py + 0.5f);
            }
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D PauseIconTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float margin = 7f;
        float barWidth = (size - margin * 2f) * 0.32f;
        float gap = (size - margin * 2f) - barWidth * 2f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x + 0.5f - margin;
            bool inBar = (px >= 0f && px < barWidth) || (px >= barWidth + gap && px < barWidth * 2f + gap);
            bool inRow = y >= margin && y < size - margin;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, inBar && inRow ? 1f : 0f));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D RadialGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float r = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
            float a = Mathf.Clamp01(1f - r);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return tex;
    }

    static float RoundRectSdf(float px, float py, float extent, float radius)
    {
        float qx = Mathf.Abs(px) - (extent - radius);
        float qy = Mathf.Abs(py) - (extent - radius);
        return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }

    // Grayscale beveled ring, 9-sliced and tinted at use time. The bevel band is shaded
    // by the SDF normal against a top-left light, so "raised" frames catch light on top
    // and "inset" frames on the bottom
    static Texture2D BevelFrameTexture(int size, bool raised)
    {
        Color outline = new Color(0.02f, 0.02f, 0.03f);
        Color innerLine = new Color(0.10f, 0.10f, 0.13f);
        float half = size * 0.5f;
        float radius = T.cornerRadius;
        float extent = half - 1f;
        Vector2 lightDir = new Vector2(-0.45f, 0.9f).normalized;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x + 0.5f - half;
            float py = y + 0.5f - half;
            float d = RoundRectSdf(px, py, extent, radius);

            float wOutline = Band(d, -1f, 0f);
            float wBevel = Band(d, -5f, -1f);
            float wInner = Band(d, -6f, -5f);
            float alpha = wOutline + wBevel + wInner;

            Color c = Color.clear;
            if (alpha > 0.001f)
            {
                float gx = RoundRectSdf(px + 1f, py, extent, radius) - RoundRectSdf(px - 1f, py, extent, radius);
                float gy = RoundRectSdf(px, py + 1f, extent, radius) - RoundRectSdf(px, py - 1f, extent, radius);
                Vector2 normal = new Vector2(gx, gy).normalized;
                float lit = Vector2.Dot(normal, lightDir);
                if (!raised) lit = -lit;
                float shade = Mathf.Clamp01(0.72f + T.bevel * lit);
                Color bevel = new Color(shade, shade, shade);

                c = (outline * wOutline + bevel * wBevel + innerLine * wInner) / alpha;
                c.a = Mathf.Clamp01(alpha);
            }
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static float Band(float d, float from, float to)
    {
        return Mathf.Clamp01(0.5f - (d - to)) * (1f - Mathf.Clamp01(0.5f - (d - from)));
    }

    // Vertical gradient with a subtle sheen along the top edge
    static Texture2D WindowFillTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color c = Color.Lerp(T.fillBottom, T.fillTop, t);
            if (t > 0.94f) c = Color.Lerp(c, c * 1.6f, (t - 0.94f) / 0.06f * 0.6f);
            c.a = 1f;
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D CircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude - (half - 1f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - dist)));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GradientTexture(int w, int h, Color top, Color bottom)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static Sprite MakeSprite(string assetPath, Texture2D tex, Vector4 border)
    {
        string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // ---------- slot prefabs ----------

    static SlotPrefabs BuildSlotPrefabs()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, PrefDir.Substring("Assets/".Length)));
        AssetDatabase.Refresh();

        SlotPrefabs prefabs = new SlotPrefabs();

        // Roulette slot: inset box, button tints the portrait (claimed = dimmed)
        {
            var (root, _, _) = Box(null, "RouletteSlotClean", T.slotFill, T.frameSecondary, true);
            root.sizeDelta = new Vector2(150, 150);
            RectTransform portrait = NewRect("Portrait", root); StretchInset(portrait, 8);
            Image portraitImg = AddImage(portrait, Color.white, null, false);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = portraitImg;
            button.colors = Colors(Color.white, Hex("FFF4D8"), Hex("C0B8A4"), new Color(0.4f, 0.4f, 0.44f, 1f));

            RectTransform badge = NewRect("Badge", root);
            badge.anchorMin = new Vector2(0, 0);
            badge.anchorMax = new Vector2(1, 0);
            badge.pivot = new Vector2(0.5f, 0);
            badge.offsetMin = new Vector2(7, 7);
            badge.offsetMax = new Vector2(-7, 43);
            AddImage(badge, new Color(0f, 0f, 0f, 0.72f), null, false);
            TMP_Text badgeText = NewText(badge, "Label", "", 18, T.textPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            StretchInset((RectTransform)badgeText.transform, 0);

            RouletteSlotButton slot = root.gameObject.AddComponent<RouletteSlotButton>();
            slot.portraitImage = portraitImg;
            slot.button = button;
            slot.badgeLabel = badgeText;
            root.gameObject.AddComponent<CharacterInspectTrigger>();

            prefabs.rouletteSlot = SavePrefab(root.gameObject, $"{PrefDir}/RouletteSlotClean.prefab");
        }

        // Champion slot: frame is the always-raycastable drop target; the highlight is a
        // raised border overlay so drag targets glow
        {
            var (root, _, _) = Box(null, "ChampionSlotClean", T.slotFill, T.frameSecondary, true);
            root.sizeDelta = new Vector2(170, 170);
            RectTransform portrait = NewRect("Portrait", root); StretchInset(portrait, 8);
            Image portraitImg = AddImage(portrait, new Color(1, 1, 1, 0), null, false);
            RectTransform highlight = NewRect("Highlight", root); StretchInset(highlight, 0);
            Image highlightImg = AddImage(highlight, Color.white, raisedFrameSprite, false);

            // hold-to-use progress along the top edge (e.g. Nostalgia)
            RectTransform holdBar = NewRect("HoldBar", root);
            holdBar.anchorMin = new Vector2(0, 1);
            holdBar.anchorMax = new Vector2(1, 1);
            holdBar.pivot = new Vector2(0.5f, 1);
            holdBar.offsetMin = new Vector2(10, -18);
            holdBar.offsetMax = new Vector2(-10, -8);
            AddImage(holdBar, Hex("050508"), null, false);
            RectTransform holdFillRt = NewRect("Fill", holdBar);
            StretchInset(holdFillRt, 1);
            Image holdFillImg = AddImage(holdFillRt, T.accentBright, barFillSprite, false);
            holdFillImg.type = Image.Type.Filled;
            holdFillImg.fillMethod = Image.FillMethod.Horizontal;
            holdFillImg.fillAmount = 0f;
            holdBar.gameObject.SetActive(false);

            ChampionSlotUI slot = root.gameObject.AddComponent<ChampionSlotUI>();
            slot.portraitImage = portraitImg;
            slot.highlightImage = highlightImg;
            slot.holdBar = holdBar.gameObject;
            slot.holdFill = holdFillImg;
            root.gameObject.AddComponent<CharacterInspectTrigger>();

            GameObject asset = SavePrefab(root.gameObject, $"{PrefDir}/ChampionSlotClean.prefab");
            prefabs.championSlot = asset.GetComponent<ChampionSlotUI>();
        }

        // Item slot: inset box + icon, hover tooltip + draggable, drop target for reordering
        {
            var (root, _, _) = Box(null, "ItemSlotClean", T.slotFill, T.frameSecondary, true);
            root.sizeDelta = new Vector2(92, 92);
            RectTransform icon = NewRect("Icon", root); StretchInset(icon, 8);
            Image iconImg = AddImage(icon, new Color(1, 1, 1, 0), null, false);

            // hold-to-forge progress bar along the TOP edge of the slot - the cursor
            // tooltip opens below-right of the pointer, so the top stays visible
            RectTransform combineBar = NewRect("CombineBar", root);
            combineBar.anchorMin = new Vector2(0, 1);
            combineBar.anchorMax = new Vector2(1, 1);
            combineBar.pivot = new Vector2(0.5f, 1);
            combineBar.offsetMin = new Vector2(6, -12);
            combineBar.offsetMax = new Vector2(-6, -5);
            AddImage(combineBar, Hex("050508"), null, false);
            RectTransform combineFillRt = NewRect("Fill", combineBar);
            StretchInset(combineFillRt, 1);
            // needs a sprite: a Filled Image with no sprite ignores fillAmount and always renders full
            Image combineFillImg = AddImage(combineFillRt, T.accentBright, barFillSprite, false);
            combineFillImg.type = Image.Type.Filled;
            combineFillImg.fillMethod = Image.FillMethod.Horizontal;
            combineFillImg.fillAmount = 0f;
            combineBar.gameObject.SetActive(false);

            ItemTooltipTrigger tooltip = root.gameObject.AddComponent<ItemTooltipTrigger>();
            ItemSlotUI slot = root.gameObject.AddComponent<ItemSlotUI>();
            slot.iconImage = iconImg;
            slot.tooltipTrigger = tooltip;
            slot.combineBar = combineBar.gameObject;
            slot.combineFill = combineFillImg;

            GameObject asset = SavePrefab(root.gameObject, $"{PrefDir}/ItemSlotClean.prefab");
            prefabs.itemSlot = asset.GetComponent<ItemSlotUI>();
        }

        return prefabs;
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return asset;
    }

    // ---------- scene UI ----------

    static UIRefs BuildUI()
    {
        UIRefs ui = new UIRefs();

        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;
        ui.canvas = canvasGO.GetComponent<Canvas>();
        ui.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGO.transform;

        RectTransform bg = NewRect("Background", canvas);
        StretchInset(bg, 0);
        AddImage(bg, Color.white, gradientSprite, false);

        BuildTopBar(canvas, ui);
        BuildPartyPanel(canvas, ui);
        BuildBenchAndBank(canvas, ui);
        BuildGlobalItemsPanel(canvas, ui, new Vector2(466, -72), true);
        BuildOtherItemsPanel(canvas, ui, new Vector2(920, -72));
        BuildShopBar(canvas, ui);
        BuildCharacterInfoPanel(canvas, ui);
        BuildRecipePanel(canvas, ui);
        BuildArtisanPanel(canvas, ui);
        BuildDialogue(canvas, ui);
        BuildTooltip(canvas, ui);

        ui.floatingText = NewText(canvas, "FloatingTextTemplate", "LEVEL UP!", 26, T.accent, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform floatRt = (RectTransform)ui.floatingText.transform;
        floatRt.sizeDelta = new Vector2(420, 44);
        ui.floatingText.characterSpacing = 3;
        ui.floatingText.gameObject.SetActive(false);

        RectTransform ghost = NewRect("DragGhost", canvas);
        ghost.sizeDelta = new Vector2(110, 110);
        ui.dragGhost = AddImage(ghost, Color.white, null, false);
        ui.dragGhost.enabled = false;

        return ui;
    }

    static void BuildTopBar(Transform canvas, UIRefs ui)
    {
        var (bar, _, _) = Window(canvas, "TopBar");
        bar.anchorMin = new Vector2(0, 1);
        bar.anchorMax = new Vector2(1, 1);
        bar.pivot = new Vector2(0.5f, 1);
        bar.offsetMin = new Vector2(10, -64);
        bar.offsetMax = new Vector2(-10, -8);

        ui.openOthersButton = TopBarButton(bar, "OthersButton", "MAP", -764, 90);
        ui.openGlobalsButton = TopBarButton(bar, "GlobalsButton", "GLOBALS", -640, 110);
        ui.openShopButton = TopBarButton(bar, "RouletteButton", "ROULETTE", -502, 130);
        ui.openPartyButton = TopBarButton(bar, "PartyButton", "PARTY", -398);
        ui.openBenchButton = TopBarButton(bar, "BenchButton", "BENCH", -294);
        ui.openBankButton = TopBarButton(bar, "EquipButton", "EQUIP", -190);

        var (chip, _, _) = Box(bar, "GoldChip", T.slotFill, T.frameSecondary, false);
        ui.goldChip = chip;
        chip.anchorMin = new Vector2(1, 0.5f);
        chip.anchorMax = new Vector2(1, 0.5f);
        chip.pivot = new Vector2(1, 0.5f);
        chip.anchoredPosition = new Vector2(-20, 0);
        chip.sizeDelta = new Vector2(150, 42);

        RectTransform coin = NewRect("Coin", chip);
        coin.anchorMin = new Vector2(0, 0.5f);
        coin.anchorMax = new Vector2(0, 0.5f);
        coin.pivot = new Vector2(0, 0.5f);
        coin.anchoredPosition = new Vector2(12, 0);
        coin.sizeDelta = new Vector2(18, 18);
        AddImage(coin, T.accent, circleSprite, false);

        ui.goldLabel = NewText(chip, "GoldLabel", "0g", 18, T.accent, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        RectTransform goldRt = (RectTransform)ui.goldLabel.transform;
        StretchInset(goldRt, 0);
        goldRt.offsetMin = new Vector2(40, 0);
        goldRt.offsetMax = new Vector2(-14, 0);
    }

    static Button TopBarButton(RectTransform bar, string name, string label, float x, float width = 96)
    {
        Button b = NewButton(bar, name, label, new Vector2(width, 40), 15, T.textPrimary, StandardFrameColors());
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        b.GetComponentInChildren<TMP_Text>().characterSpacing = 2;
        return b;
    }

    static void BuildPartyPanel(Transform canvas, UIRefs ui)
    {
        var (panel, _, _) = Window(canvas, "PartyPanel");
        SetTopLeft(panel, new Vector2(10, -72), new Vector2(440, 490));
        ui.partyPanel = panel.gameObject;

        SectionTitle(panel, "PARTY", -16);
        SectionLabel(panel, "FRONTLINE", -58);
        ui.frontlineParent = SlotGrid(panel, "FrontlineSlots", -82, 170, 12, 2, 352, 170);
        SectionLabel(panel, "BACKLINE", -272);
        ui.backlineParent = SlotGrid(panel, "BacklineSlots", -296, 170, 12, 2, 352, 170);
    }

    static void BuildBenchAndBank(Transform canvas, UIRefs ui)
    {
        var (bench, _, _) = Window(canvas, "BenchPanel");
        SetTopRight(bench, new Vector2(-10, -72), new Vector2(440, 272));
        ui.benchPanel = bench.gameObject;
        SectionTitle(bench, "BENCH", -16);
        ui.benchSlotsParent = SlotGrid(bench, "BenchSlots", -58, 92, 10, 4, 398, 194);

        var (bank, _, _) = Window(canvas, "BankPanel");
        SetTopRight(bank, new Vector2(-10, -352), new Vector2(440, 272));
        ui.bankPanel = bank.gameObject;
        SectionTitle(bank, "EQUIPMENT", -16);
        ui.bankSlotsParent = SlotGrid(bank, "BankSlots", -58, 92, 10, 4, 398, 194);
    }

    static void BuildShopBar(Transform canvas, UIRefs ui)
    {
        var (bar, _, _) = Window(canvas, "ShopBar");
        bar.anchorMin = new Vector2(0, 0);
        bar.anchorMax = new Vector2(1, 0);
        bar.pivot = new Vector2(0.5f, 0);
        bar.offsetMin = new Vector2(10, 8);
        bar.offsetMax = new Vector2(-10, 204);
        ui.shopPanel = bar.gameObject;

        SectionTitle(bar, "ROULETTE", -14);

        ui.commonButton = RarityButton(bar, "CommonButton", "Common", CommonColor, 24);
        ui.epicButton = RarityButton(bar, "EpicButton", "Epic", EpicColor, 152);
        ui.legendaryButton = RarityButton(bar, "LegendaryButton", "Legendary", LegendaryColor, 280);

        ui.spinButton = NewButton(bar, "SpinButton", "Spin", new Vector2(324, 58), 22, T.accent,
            Colors(T.accent, T.accentBright, T.accentPressed, T.frameDisabled));
        RectTransform spinRt = (RectTransform)ui.spinButton.transform;
        spinRt.anchorMin = new Vector2(0, 1);
        spinRt.anchorMax = new Vector2(0, 1);
        spinRt.pivot = new Vector2(0, 1);
        spinRt.anchoredPosition = new Vector2(24, -104);
        ui.spinCostLabel = ui.spinButton.GetComponentInChildren<TMP_Text>();
        ui.spinCostLabel.fontStyle = FontStyles.Bold;
        ui.spinCostLabel.characterSpacing = 2;

        ui.feedbackLabel = NewText(bar, "FeedbackLabel", "", 16, Hex("F87060"), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)ui.feedbackLabel.transform, new Vector2(364, -110), new Vector2(330, 46));

        RectTransform slots = NewRect("RouletteSlots", bar);
        slots.anchorMin = new Vector2(0.5f, 0.5f);
        slots.anchorMax = new Vector2(0.5f, 0.5f);
        slots.pivot = new Vector2(0.5f, 0.5f);
        slots.anchoredPosition = new Vector2(170, -2);
        slots.sizeDelta = new Vector2(806, 150);
        Grid(slots, 150, 14, 5);
        ui.rouletteSlotsParent = slots;
    }

    static Button RarityButton(RectTransform bar, string name, string label, Color rarity, float x)
    {
        // RouletteUIManager disables the selected rarity button, so the disabled frame
        // color doubles as the "selected" state - a lit accent border
        Button b = NewButton(bar, name, label, new Vector2(120, 42), 14, T.textPrimary,
            Colors(T.framePrimary, T.accentBright, T.accentPressed, T.accent));
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -48);

        RectTransform dot = NewRect("Dot", rt);
        dot.anchorMin = new Vector2(0, 0.5f);
        dot.anchorMax = new Vector2(0, 0.5f);
        dot.pivot = new Vector2(0, 0.5f);
        dot.anchoredPosition = new Vector2(12, 0);
        dot.sizeDelta = new Vector2(9, 9);
        AddImage(dot, rarity, circleSprite, false);

        TMP_Text text = b.GetComponentInChildren<TMP_Text>();
        RectTransform textRt = (RectTransform)text.transform;
        textRt.offsetMin = new Vector2(24, 0);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return b;
    }

    static void BuildCharacterInfoPanel(Transform canvas, UIRefs ui)
    {
        // The dim overlay does NOT block raycasts - the item bank stays reachable so
        // items can be dragged straight into the equipment slots below
        RectTransform overlay = NewRect("CharacterInfoPanel", canvas);
        StretchInset(overlay, 0);
        AddImage(overlay, new Color(0f, 0f, 0.01f, 0.45f), null, false);
        ui.infoPanelRoot = overlay.gameObject;

        var (win, winFill, _) = Window(overlay, "Window");
        win.anchorMin = new Vector2(0.5f, 0.5f);
        win.anchorMax = new Vector2(0.5f, 0.5f);
        win.pivot = new Vector2(0.5f, 0.5f);
        win.anchoredPosition = Vector2.zero;
        win.sizeDelta = new Vector2(720, 640);
        winFill.raycastTarget = true;

        ui.infoCloseButton = NewButton(win, "CloseButton", "X", new Vector2(38, 38), 18, T.textPrimary, StandardFrameColors());
        RectTransform closeRt = (RectTransform)ui.infoCloseButton.transform;
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.anchoredPosition = new Vector2(-16, -16);

        var (portraitFrame, _, _) = Box(win, "PortraitFrame", T.slotFill, T.framePrimary, false);
        SetTopLeft(portraitFrame, new Vector2(28, -28), new Vector2(120, 120));
        RectTransform portrait = NewRect("Portrait", portraitFrame); StretchInset(portrait, 7);
        ui.infoPortrait = AddImage(portrait, Color.white, null, false);

        ui.infoName = NewText(win, "NameLabel", "Champion", 30, T.textPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)ui.infoName.transform, new Vector2(164, -30), new Vector2(430, 40));

        var (levelChip, _, _) = Box(win, "LevelChip", T.slotFill, T.frameSecondary, false);
        SetTopLeft(levelChip, new Vector2(164, -78), new Vector2(88, 34));
        ui.infoLevel = NewText(levelChip, "LevelLabel", "Lv. 1", 17, T.accent, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)ui.infoLevel.transform, 0);

        ui.infoMemento = NewText(win, "MementoLabel", "0/3", 15, T.textSecondary, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        SetTopLeft((RectTransform)ui.infoMemento.transform, new Vector2(266, -78), new Vector2(140, 34));

        SectionLabel(win, "ATTRIBUTES", -174, 28);
        RectTransform stats = NewRect("Stats", win);
        SetTopLeft(stats, new Vector2(28, -198), new Vector2(664, 132));
        GridLayoutGroup statsGrid = stats.gameObject.AddComponent<GridLayoutGroup>();
        statsGrid.cellSize = new Vector2(330, 30);
        statsGrid.spacing = new Vector2(4, 4);
        statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        statsGrid.constraintCount = 2;

        ui.hp = StatText(stats, "HPLabel");
        ui.ad = StatText(stats, "ADLabel");
        ui.ap = StatText(stats, "APLabel");
        ui.atkSpd = StatText(stats, "AtkSpdLabel");
        ui.crit = StatText(stats, "CritLabel");
        ui.def = StatText(stats, "DefLabel");
        ui.mdef = StatText(stats, "MDefLabel");
        ui.resource = StatText(stats, "ResourceLabel");

        SectionLabel(win, "ABILITIES", -344, 28);
        ui.frontAbilityRow = AbilityRow(win, "FrontlineAbilityRow", -368);
        ui.backAbilityRow = AbilityRow(win, "BacklineAbilityRow", -486);

        SectionLabel(win, "EQUIPMENT", -344, 362);
        ui.equipSlots = new EquippedItemSlotUI[3];
        for (int i = 0; i < 3; i++)
            ui.equipSlots[i] = EquipSlot(win, $"EquipSlot{i + 1}", new Vector2(362 + i * 90, -368));
    }

    static AbilityRowUI AbilityRow(RectTransform win, string name, float y)
    {
        RectTransform row = NewRect(name, win);
        SetTopLeft(row, new Vector2(28, y), new Vector2(350, 110));
        AbilityRowUI rowUI = row.gameObject.AddComponent<AbilityRowUI>();

        var (chip, _, _) = Box(row, "PositionChip", T.slotFill, T.frameSecondary, false);
        SetTopLeft(chip, new Vector2(0, -37), new Vector2(36, 36));
        rowUI.positionLabel = NewText(chip, "PositionLabel", "F", 16, T.accent, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchInset((RectTransform)rowUI.positionLabel.transform, 0);

        var (iconFrame, _, _) = Box(row, "IconFrame", T.slotFill, T.frameSecondary, false);
        SetTopLeft(iconFrame, new Vector2(50, 0), new Vector2(110, 110));
        RectTransform icon = NewRect("Icon", iconFrame); StretchInset(icon, 9);
        Image iconImg = AddImage(icon, Color.white, null, true);

        TMP_Text marker = NewText(row, "ActiveMarker", "< ACTIVE", 16, T.accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)marker.transform, new Vector2(172, -40), new Vector2(160, 30));
        marker.characterSpacing = 2;
        marker.gameObject.SetActive(false);

        rowUI.iconImage = iconImg;
        rowUI.iconTooltipTrigger = icon.gameObject.AddComponent<AbilityTooltipTrigger>();
        rowUI.activeMarker = marker.gameObject;
        return rowUI;
    }

    static EquippedItemSlotUI EquipSlot(RectTransform win, string name, Vector2 pos)
    {
        var (root, _, _) = Box(win, name, T.slotFill, T.frameSecondary, true);
        SetTopLeft(root, pos, new Vector2(76, 76));
        RectTransform icon = NewRect("Icon", root); StretchInset(icon, 8);
        Image iconImg = AddImage(icon, new Color(1, 1, 1, 0), null, false);

        ItemTooltipTrigger tooltip = root.gameObject.AddComponent<ItemTooltipTrigger>();
        EquippedItemSlotUI slot = root.gameObject.AddComponent<EquippedItemSlotUI>();
        slot.iconImage = iconImg;
        slot.tooltipTrigger = tooltip;
        return slot;
    }

    static void BuildDialogue(Transform canvas, UIRefs ui)
    {
        RectTransform wrapper = NewRect("DialogueUI", canvas);
        StretchInset(wrapper, 0);
        ui.dialogue = wrapper.gameObject.AddComponent<DialogueUIController>();

        var (panel, panelFill, _) = Window(wrapper, "Panel");
        panel.anchorMin = new Vector2(0.5f, 0);
        panel.anchorMax = new Vector2(0.5f, 0);
        panel.pivot = new Vector2(0.5f, 0);
        panel.anchoredPosition = new Vector2(0, 212);
        panel.sizeDelta = new Vector2(820, 150);
        panelFill.raycastTarget = true;

        var (portraitFrame, _, _) = Box(panel, "PortraitFrame", T.slotFill, T.framePrimary, false);
        SetTopLeft(portraitFrame, new Vector2(18, -18), new Vector2(114, 114));
        RectTransform portrait = NewRect("Portrait", portraitFrame); StretchInset(portrait, 6);
        Image portraitImg = AddImage(portrait, Color.white, null, false);

        TMP_Text nameLabel = NewText(panel, "NameLabel", "Name", 21, T.accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)nameLabel.transform, new Vector2(152, -18), new Vector2(500, 28));

        TMP_Text quote = NewText(panel, "QuoteLabel", "...", 18, T.textPrimary, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        SetTopLeft((RectTransform)quote.transform, new Vector2(152, -52), new Vector2(636, 80));

        TMP_Text hint = NewText(panel, "Hint", ">>", 14, T.accent, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        RectTransform hintRt = (RectTransform)hint.transform;
        hintRt.anchorMin = new Vector2(1, 0);
        hintRt.anchorMax = new Vector2(1, 0);
        hintRt.pivot = new Vector2(1, 0);
        hintRt.anchoredPosition = new Vector2(-18, 12);
        hintRt.sizeDelta = new Vector2(50, 20);

        ui.dialogue.panelRoot = panel.gameObject;
        ui.dialogue.portraitImage = portraitImg;
        ui.dialogue.nameLabel = nameLabel;
        ui.dialogue.quoteLabel = quote;
    }

    static void BuildTooltip(Transform canvas, UIRefs ui)
    {
        var (panel, _, _) = Window(canvas, "AbilityTooltipPanel");
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0, 1);
        panel.sizeDelta = new Vector2(330, 100);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ui.tooltipTitle = NewText(panel, "TitleLabel", "Title", 18, T.accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        ui.tooltipDescription = NewText(panel, "DescriptionLabel", "Description", 16, T.textPrimary, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        ui.tooltipRoot = panel.gameObject;
    }

    // ---------- wiring ----------

    static void WireManagers(UIRefs ui, SlotPrefabs prefabs)
    {
        RouletteUIManager roulette = Object.FindObjectOfType<RouletteUIManager>(true);
        roulette.slotsParent = ui.rouletteSlotsParent;
        roulette.slotButtonPrefab = prefabs.rouletteSlot;
        roulette.goldLabel = ui.goldLabel;
        roulette.setCommonButton = ui.commonButton;
        roulette.setEpicButton = ui.epicButton;
        roulette.setLegendaryButton = ui.legendaryButton;
        roulette.spinButton = ui.spinButton;
        roulette.spinCostLabel = ui.spinCostLabel;

        UIPanelToggle shopToggle = ui.openShopButton.gameObject.AddComponent<UIPanelToggle>();
        shopToggle.button = ui.openShopButton;
        shopToggle.panel = ui.shopPanel;

        // Both live on the Canvas so they keep running even when the roulette window is hidden
        RouletteManager rouletteManager = Object.FindObjectOfType<RouletteManager>(true);

        RouletteFeedbackUI feedback = ui.canvas.gameObject.AddComponent<RouletteFeedbackUI>();
        feedback.roulette = rouletteManager;
        feedback.shakeTarget = (RectTransform)ui.spinButton.transform;
        feedback.flashGraphic = ui.spinButton.targetGraphic;
        feedback.messageLabel = ui.feedbackLabel;

        RouletteJuiceUI juice = ui.canvas.gameObject.AddComponent<RouletteJuiceUI>();
        juice.roulette = rouletteManager;
        juice.playerGold = Object.FindObjectOfType<PlayerGold>(true);
        juice.slotsParent = ui.rouletteSlotsParent;
        juice.goldChip = ui.goldChip;
        juice.floatingTextTemplate = ui.floatingText;
        juice.sparkColors = new[] { T.accentBright, T.accent, new Color(0.95f, 0.95f, 0.9f), T.framePrimary };

        PartyBenchUIController partyBench = Object.FindObjectOfType<PartyBenchUIController>(true);
        partyBench.benchPanel = ui.benchPanel;
        partyBench.partyPanel = ui.partyPanel;
        partyBench.benchSlotsParent = ui.benchSlotsParent;
        partyBench.partyFrontlineParent = ui.frontlineParent;
        partyBench.partyBacklineParent = ui.backlineParent;
        partyBench.slotPrefab = prefabs.championSlot;
        partyBench.openBenchButton = ui.openBenchButton;
        partyBench.openPartyButton = ui.openPartyButton;
        partyBench.rootCanvas = ui.canvas;
        partyBench.dragGhostImage = ui.dragGhost;

        ItemBankUIController bank = Object.FindObjectOfType<ItemBankUIController>(true);
        if (bank == null) bank = partyBench.gameObject.AddComponent<ItemBankUIController>();
        bank.bank = Object.FindObjectOfType<PlayerItemBank>(true);
        bank.equipmentManager = Object.FindObjectOfType<CharacterEquipmentManager>(true);
        bank.bankPanel = ui.bankPanel;
        bank.bankSlotsParent = ui.bankSlotsParent;
        bank.slotPrefab = prefabs.itemSlot;
        bank.openBankButton = ui.openBankButton;
        bank.rootCanvas = ui.canvas;
        bank.dragGhostImage = ui.dragGhost;

        // Complete items + recipe book so components can be combined in the bank here too
        CompleteItemGenerator.Generate();
        bank.bank.recipeBook = AssetDatabase.LoadAssetAtPath<ItemRecipeBook>("Assets/Items/ItemRecipeBook.asset");
        WireRecipePanel(ui);

        // Global items stash: view-only here (no manager/pause), usable in battle
        GlobalItemGenerator.Generate();
        PlayerGlobalItemBank globalBank = Object.FindObjectOfType<PlayerGlobalItemBank>(true);
        if (globalBank == null) globalBank = bank.bank.gameObject.AddComponent<PlayerGlobalItemBank>();
        SeedGlobalItems(globalBank);

        GlobalItemBankUI globalUI = ui.canvas.gameObject.AddComponent<GlobalItemBankUI>();
        globalUI.bank = globalBank;
        globalUI.slots = ui.globalSlots;
        globalUI.panelGroup = ui.globalGroup;

        UIPanelToggle globalsToggle = ui.openGlobalsButton.gameObject.AddComponent<UIPanelToggle>();
        globalsToggle.button = ui.openGlobalsButton;
        globalsToggle.panel = ui.globalPanel;

        // Map consumables ("Outros Items") - map-only, so they live in this scene
        OtherItemGenerator.Generate();
        RouletteManager rouletteMgr = Object.FindObjectOfType<RouletteManager>(true);
        PlayerRoster rosterRef = Object.FindObjectOfType<PlayerRoster>(true);
        PlayerParty partyRef = Object.FindObjectOfType<PlayerParty>(true);

        ArtisanAspectPanelUI artisanUI = ui.canvas.gameObject.AddComponent<ArtisanAspectPanelUI>();
        artisanUI.panelRoot = ui.artisanPanelRoot;
        artisanUI.closeButton = ui.artisanCloseButton;
        artisanUI.choices = ui.artisanChoices;

        OtherItemManager otherManager = ui.canvas.gameObject.AddComponent<OtherItemManager>();
        otherManager.itemBank = bank.bank;
        otherManager.roster = rosterRef;
        otherManager.party = partyRef;
        otherManager.championPool = rouletteMgr != null ? rouletteMgr.championPool : null;
        otherManager.dialogueUI = ui.dialogue;

        PlayerOtherItemBank otherBank = bank.bank.gameObject.AddComponent<PlayerOtherItemBank>();
        otherBank.capacity = 8;
        otherBank.debugFillOnStart = true;
        foreach (string otherName in new[] { "Disassembler", "Delirium", "Artisans Aspect", "Nostalgia" })
        {
            OtherItemData item = AssetDatabase.LoadAssetAtPath<OtherItemData>($"Assets/Items/Others/{otherName}.asset");
            if (item != null) otherBank.debugSeedItems.Add(item);
        }

        // seed a complete item so Disassembler/Delirium are testable out of the box
        ItemComponentData finesse = AssetDatabase.LoadAssetAtPath<ItemComponentData>("Assets/Items/Complete/Finésse.asset");
        if (finesse != null) bank.bank.debugSeedItems.Add(finesse);

        OtherItemBankUI otherUI = ui.canvas.gameObject.AddComponent<OtherItemBankUI>();
        otherUI.bank = otherBank;
        otherUI.manager = otherManager;
        otherUI.artisanPanel = artisanUI;
        otherUI.slots = ui.otherSlots;
        otherUI.rootCanvas = ui.canvas;
        otherUI.dragGhostImage = ui.dragGhost;

        UIPanelToggle othersToggle = ui.openOthersButton.gameObject.AddComponent<UIPanelToggle>();
        othersToggle.button = ui.openOthersButton;
        othersToggle.panel = ui.otherPanel;

        CharacterInfoPanelController info = Object.FindObjectOfType<CharacterInfoPanelController>(true);
        info.party = partyRef;
        info.panelRoot = ui.infoPanelRoot;
        info.closeButton = ui.infoCloseButton;
        info.portraitImage = ui.infoPortrait;
        info.nameLabel = ui.infoName;
        info.levelLabel = ui.infoLevel;
        info.mementoLabel = ui.infoMemento;
        info.hpLabel = ui.hp;
        info.adLabel = ui.ad;
        info.apLabel = ui.ap;
        info.atkSpeedLabel = ui.atkSpd;
        info.critLabel = ui.crit;
        info.defLabel = ui.def;
        info.mdefLabel = ui.mdef;
        info.resourceLabel = ui.resource;
        info.frontlineAbilityRow = ui.frontAbilityRow;
        info.backlineAbilityRow = ui.backAbilityRow;
        info.equipmentSlots = ui.equipSlots;

        AbilityTooltip tooltip = Object.FindObjectOfType<AbilityTooltip>(true);
        tooltip.panelRoot = ui.tooltipRoot;
        tooltip.titleLabel = ui.tooltipTitle;
        tooltip.descriptionLabel = ui.tooltipDescription;

        RouletteDialogueTrigger dialogueTrigger = Object.FindObjectOfType<RouletteDialogueTrigger>(true);
        if (dialogueTrigger != null) dialogueTrigger.dialogueUI = ui.dialogue;
    }

    // ---------- small helpers ----------

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rt = (RectTransform)go.transform;
        if (parent != null) rt.SetParent(parent, false);
        return rt;
    }

    static Image AddImage(RectTransform rt, Color color, Sprite sprite, bool raycast)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = raycast;
        return img;
    }

    // Raised window: gradient fill inset under a beveled 9-sliced frame. The frame is
    // the raycast surface when one is needed - its rect covers the whole window, and
    // pointer events bubble up to handlers on the root.
    static (RectTransform root, Image fill, Image frame) Window(Transform parent, string name)
    {
        RectTransform root = NewRect(name, parent);
        RectTransform fillRt = NewRect("Fill", root);
        StretchInset(fillRt, 4);
        Image fill = AddImage(fillRt, Color.white, fillSprite, false);
        fillRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        RectTransform frameRt = NewRect("Frame", root);
        StretchInset(frameRt, 0);
        Image frame = AddImage(frameRt, T.framePrimary, raisedFrameSprite, false);
        frameRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        return (root, fill, frame);
    }

    // Inset box: flat dark fill under a carved-in beveled frame, for slots and chips
    static (RectTransform root, Image fill, Image frame) Box(Transform parent, string name, Color fillColor, Color frameTint, bool raycast)
    {
        RectTransform root = NewRect(name, parent);
        RectTransform fillRt = NewRect("Fill", root);
        StretchInset(fillRt, 4);
        Image fill = AddImage(fillRt, fillColor, null, false);
        fillRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        RectTransform frameRt = NewRect("Frame", root);
        StretchInset(frameRt, 0);
        Image frame = AddImage(frameRt, frameTint, insetFrameSprite, raycast);
        frameRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        return (root, fill, frame);
    }

    static TMP_Text NewText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment, FontStyles style)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = alignment;
        t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    // Buttons highlight by tinting the beveled frame - hover turns the border to the accent
    static Button NewButton(Transform parent, string name, string label, Vector2 size, float fontSize, Color textColor, ColorBlock colors)
    {
        var (root, _, frame) = Window(parent, name);
        root.sizeDelta = size;
        frame.raycastTarget = true;
        frame.color = Color.white; // frame tint comes entirely from the ColorBlock
        Button btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = frame;
        btn.colors = colors;
        TMP_Text text = NewText(root, "Label", label, fontSize, textColor, TextAlignmentOptions.Center, FontStyles.Normal);
        StretchInset((RectTransform)text.transform, 0);
        return btn;
    }

    static ColorBlock StandardFrameColors()
    {
        return Colors(T.framePrimary, T.accentBright, T.accentPressed, T.frameDisabled);
    }

    static ColorBlock Colors(Color normal, Color highlighted, Color pressed, Color disabled)
    {
        return new ColorBlock
        {
            normalColor = normal,
            highlightedColor = highlighted,
            pressedColor = pressed,
            selectedColor = highlighted,
            disabledColor = disabled,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }

    static void StretchInset(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static void SetTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void SetTopRight(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void SectionTitle(RectTransform panel, string text, float y)
    {
        string accentHex = ColorUtility.ToHtmlStringRGB(T.accent);
        TMP_Text t = NewText(panel, "Title", $"<color=#{accentHex}>></color> {text}", 19, T.textPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetTopLeft((RectTransform)t.transform, new Vector2(22, y), new Vector2(300, 28));
        t.characterSpacing = 3;
    }

    static void SectionLabel(RectTransform panel, string text, float y, float x = 22)
    {
        TMP_Text t = NewText(panel, text + "Label", text, 15, T.textSecondary, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        SetTopLeft((RectTransform)t.transform, new Vector2(x, y), new Vector2(300, 20));
        t.characterSpacing = 3;
    }

    static Transform SlotGrid(RectTransform panel, string name, float y, float cell, float spacing, int cols, float width, float height)
    {
        RectTransform rt = NewRect(name, panel);
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(width, height);
        Grid(rt, cell, spacing, cols);
        return rt;
    }

    static void Grid(RectTransform rt, float cell, float spacing, int cols)
    {
        GridLayoutGroup grid = rt.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = new Vector2(spacing, spacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    static TMP_Text StatText(RectTransform parent, string name)
    {
        return NewText(parent, name, "-", 20, T.textPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
    }
}
