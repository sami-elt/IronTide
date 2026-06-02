using System.Collections;
using System.Collections.Generic;
using IronTide.BasicCards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class TestDay1PlayUI : MonoBehaviour
{
    public TextMeshProUGUI turnIndicator;
    public TextMeshProUGUI moveIndicator;
    public TextMeshProUGUI rangeIndicator;
    public TextMeshProUGUI damageIndicator;
    public TextMeshProUGUI winIndicator;
    public TextMeshProUGUI phaseIndicator;
    public List<TextMeshProUGUI> healthIndicators;
    //public List<Ship> ships;
    public List<Ship> ships = new();
    [Header("Module Test Setup")]
    [SerializeField] private IronTideModuleCardLibrary moduleLibrary;
    [SerializeField] private bool autoDealStarterModules = true;
    [SerializeField] private float roundTransitionDelay = 1.1f;

    private readonly List<PlayerHudPanel> playerPanels = new List<PlayerHudPanel>();
    private readonly List<RectTransform> hudHpDividers = new List<RectTransform>();
    private RectTransform combatHudRoot;
    private RectTransform rosterPanelRoot;
    private RectTransform victoryOverlayRoot;
    private TextMeshProUGUI hudPlayerTitle;
    private RectTransform hudHpLostFill;
    private RectTransform hudHpFill;
    private TextMeshProUGUI hudHpText;
    private TextMeshProUGUI hudRound;
    private TextMeshProUGUI hudPhase;
    private TextMeshProUGUI hudMovement;
    private TextMeshProUGUI hudAttack;
    private TextMeshProUGUI hudArmor;
    private TextMeshProUGUI hudLastAction;
    private Button moveButton;
    private Button attackButton;
    private Button endTurnButton;
    private Button rosterToggleButton;
    private TextMeshProUGUI rosterToggleText;
    private Button cameraToggleButton;
    private TextMeshProUGUI cameraToggleText;
    private CameraController cachedCameraController;
    private ModuleSlotHud activeWeaponSlot;
    private ModuleSlotHud activeArmorSlot;
    private ModuleSlotHud activeEngineSlot;
    private RectTransform moduleTooltip;
    private TextMeshProUGUI tooltipTitle;
    private TextMeshProUGUI tooltipMeta;
    private TextMeshProUGUI tooltipStats;
    private TextMeshProUGUI tooltipPassive;
    private TextMeshProUGUI victoryTitle;
    private TextMeshProUGUI victorySubtitle;
    private ModuleSlotHud hoveredSlot;

    private bool gameOver;
    private int winnerNumber;
    private bool transitionStarted;
    private string lastActionText = "Waiting for first move.";
    private string attackText = "Attack not rolled.";
    private string previousAttackText = string.Empty;
    private bool rosterOpen;
    private HudSprites hudSprites;

    private static readonly Color PanelColor = new Color(0.045f, 0.047f, 0.050f, 0.96f);
    private static readonly Color HeaderColor = new Color(0.12f, 0.10f, 0.09f, 0.97f);
    private static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.32f, 1f);
    private static readonly Color TextColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.70f, 0.78f, 1f);
    private static readonly Color WeaponModuleColor = new Color(0.30f, 0.11f, 0.08f, 0.98f);
    private static readonly Color ArmorModuleColor = new Color(0.10f, 0.17f, 0.25f, 0.98f);
    private static readonly Color EngineModuleColor = new Color(0.08f, 0.24f, 0.22f, 0.98f);
    private static readonly Color ActiveModuleColor = new Color(0.055f, 0.060f, 0.065f, 0.98f);
    private static readonly Color BrokenModuleColor = new Color(0.46f, 0.06f, 0.045f, 0.82f);
    private static readonly Color EmptyModuleColor = new Color(0.07f, 0.09f, 0.12f, 0.86f);
    private static readonly Color TooltipColor = new Color(0.04f, 0.06f, 0.09f, 0.98f);
    private static readonly Color HpHealthyColor = new Color(0.22f, 0.78f, 0.32f, 1f);
    private static readonly Color HpLostColor = new Color(0.30f, 0.34f, 0.34f, 1f);
    private static readonly Color HpDividerColor = new Color(0.98f, 0.96f, 0.86f, 0.95f);
    private const string GoldHex = "#F2C84B";
    private const string MoveHex = "#89D8FF";
    private const string TextHex = "#EAF2FF";
    private const string MutedHex = "#A6B6C9";
    private const string DangerHex = "#F05C45";
    private const string MainMenuSceneName = "GameDemo";

    private void Awake()
    {
        ResolveModuleLibrary();
        ResolveShipReferences();
        //SetupPlayerModules();
    }

    private void Start()
    {
        HideLegacyLabels();
        BuildHud();

        TurnManager.OnTurnStarted += HandleTurnStarted;
        TurnManager.OnMovementRolled += HandleMovementRolled;
        TurnManager.OnAttackRolled += HandleAttackRolled;
        TurnManager.OnDamageDealt += HandleDamageDealt;
        TurnManager.OnTurnFeedback += HandleTurnFeedback;
        TurnManager.OnAttackResolved += HandleAttackResolved;
        TurnManager.OnAttackPrepared += HandleAttackPrepared;

        foreach (Ship ship in ships)
        {
            if (ship != null && ship.shipInfo != null)
                ship.shipInfo.OnModuleStateChanged += RefreshAllModulePanels;
        }

        RefreshAllModulePanels();
    }

    private void OnDestroy()
    {
        TurnManager.OnTurnStarted -= HandleTurnStarted;
        TurnManager.OnMovementRolled -= HandleMovementRolled;
        TurnManager.OnAttackRolled -= HandleAttackRolled;
        TurnManager.OnDamageDealt -= HandleDamageDealt;
        TurnManager.OnTurnFeedback -= HandleTurnFeedback;
        TurnManager.OnAttackResolved -= HandleAttackResolved;
        TurnManager.OnAttackPrepared -= HandleAttackPrepared;

        foreach (Ship ship in ships)
        {
            if (ship != null && ship.shipInfo != null)
                ship.shipInfo.OnModuleStateChanged -= RefreshAllModulePanels;
        }
    }

    private void Update()
    {
        UpdateWinState();
        RefreshCombatHud();
        RefreshAllModulePanels();
    }

    private void ResolveModuleLibrary()
    {
        if (moduleLibrary != null)
            return;

        moduleLibrary = Resources.Load<IronTideModuleCardLibrary>("IronTideModuleLibrary");

#if UNITY_EDITOR
        if (moduleLibrary == null)
        {
            moduleLibrary = UnityEditor.AssetDatabase.LoadAssetAtPath<IronTideModuleCardLibrary>(
                "Assets/IronTide/BasicCards/Data/IronTideModuleLibrary.asset");
        }
#endif
    }

    private void ResolveShipReferences()
    {
        foreach (Ship ship in ships)
        {
            if (ship == null)
                continue;

            if (ship.shipInfo == null)
                ship.shipInfo = ship.GetComponent<ShipInfo>();
            if (ship.shipMovement == null)
                ship.shipMovement = ship.GetComponent<ShipMovement>();
            if (ship.shipWeapon == null)
                ship.shipWeapon = ship.GetComponent<ShipWeapon>();
            if (ship.turnPlayerController == null)
                ship.turnPlayerController = ship.GetComponent<TurnPlayerController>();
        }
    }

    private void SetupPlayerModules()
    {
        IronTideGameState.EnsurePlayers(ships.Count);

        if (ShouldRestoreSavedModules())
        {
            RestoreSavedModules();
            return;
        }

        if (autoDealStarterModules)
            DealStarterWeapons();
    }

    private bool ShouldRestoreSavedModules()
    {
        if (!IronTideGameState.HasSavedLoadouts)
            return false;

        if (!IronTideGameState.ShouldOpenShopAfterCombat)
            return true;

        for (int i = 0; i < IronTideGameState.Players.Count; i++)
        {
            IronTidePlayerState player = IronTideGameState.GetPlayer(i);
            if (player != null && IsValidSavedWeapon(player.WeaponModuleId))
                return true;
        }

        return false;
    }

    private bool IsValidSavedWeapon(string moduleId)
    {
        if (moduleLibrary == null)
            return false;

        IronTideModuleCardEntry card = moduleLibrary.FindById(moduleId);
        return card != null && card.IsValid && card.SlotType == BasicModuleType.Weapon;
    }

    private void RestoreSavedModules()
    {
        for (int i = 0; i < ships.Count; i++)
        {
            Ship ship = ships[i];
            IronTidePlayerState player = IronTideGameState.GetPlayer(i);
            IronTideGameState.ApplyLoadoutToShip(ship, player, moduleLibrary);
        }
    }

    private void DealStarterWeapons()
    {
        if (moduleLibrary == null)
        {
            Debug.LogWarning("TestDay1PlayUI could not find IronTideModuleLibrary. Module HUD will show empty slots.");
            return;
        }

        var weapons = Shuffled(moduleLibrary.GetTier1Cards(BasicModuleType.Weapon));
        RemoveAlreadyEquippedWeapons(weapons);
        int weaponIndex = 0;

        for (int i = 0; i < ships.Count; i++)
        {
            Ship ship = ships[i];
            if (ship == null || ship.shipInfo == null)
                continue;

            ShipInfo info = ship.shipInfo;
            if ((info.WeaponModule == null || !info.WeaponModule.IsValid) && weaponIndex < weapons.Count)
            {
                info.SetWeaponModule(weapons[weaponIndex]);
                weaponIndex++;
            }

            info.SetArmorModule(null);
            info.SetEngineModule(null);

            info.ResetValues();
        }

        IronTideGameState.SaveLoadouts(ships);
    }

    private void RemoveAlreadyEquippedWeapons(List<IronTideModuleCardEntry> weapons)
    {
        for (int i = weapons.Count - 1; i >= 0; i--)
        {
            IronTideModuleCardEntry weapon = weapons[i];
            if (weapon == null)
                continue;

            foreach (Ship ship in ships)
            {
                if (ship == null || ship.shipInfo == null || ship.shipInfo.WeaponModule != weapon)
                    continue;

                weapons.RemoveAt(i);
                break;
            }
        }
    }

    private void EnsureEventSystem()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
            Destroy(standaloneInputModule);
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static List<IronTideModuleCardEntry> Shuffled(List<IronTideModuleCardEntry> source)
    {
        var results = new List<IronTideModuleCardEntry>(source);
        for (int i = results.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            IronTideModuleCardEntry temp = results[i];
            results[i] = results[randomIndex];
            results[randomIndex] = temp;
        }

        return results;
    }

    private void HideLegacyLabels()
    {
        if (moveIndicator != null)
            moveIndicator.gameObject.SetActive(false);
        if (rangeIndicator != null)
            rangeIndicator.gameObject.SetActive(false);
        if (damageIndicator != null)
            damageIndicator.gameObject.SetActive(false);

        foreach (TextMeshProUGUI label in healthIndicators)
        {
            if (label != null)
                label.gameObject.SetActive(false);
        }

        if (turnIndicator != null)
            turnIndicator.gameObject.SetActive(false);
        if (phaseIndicator != null)
            phaseIndicator.gameObject.SetActive(false);

        if (winIndicator != null)
            winIndicator.gameObject.SetActive(false);

        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.text))
                continue;

            if (label.text.Contains("Press M to proceed"))
                label.gameObject.SetActive(false);
        }
    }

    private void BuildHud()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();
        hudSprites = HudSprites.Create();
        BuildCombatHud(transform);
        BuildModuleTooltip(transform);
        BuildVictoryOverlay(transform);
    }

    private void BuildCombatHud(Transform parent)
    {
        combatHudRoot = CreatePanel("Combat Command HUD", parent, PanelColor);
        combatHudRoot.anchorMin = new Vector2(0f, 0f);
        combatHudRoot.anchorMax = new Vector2(1f, 0f);
        combatHudRoot.pivot = new Vector2(0.5f, 0f);
        combatHudRoot.offsetMin = new Vector2(14f, 10f);
        combatHudRoot.offsetMax = new Vector2(-14f, 160f);
        ApplyHudImage(combatHudRoot, hudSprites.Panel, Image.Type.Sliced, new Color(0.055f, 0.052f, 0.050f, 0.92f), true);
        AddHudOutline(combatHudRoot, new Color(0.02f, 0.018f, 0.016f, 0.82f), new Vector2(3f, -3f));

        var mainLayout = combatHudRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        mainLayout.padding = new RectOffset(10, 10, 8, 8);
        mainLayout.spacing = 8;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = false;
        mainLayout.childForceExpandHeight = true;

        BuildCommandPanel(combatHudRoot);
        BuildActiveModuleStrip(combatHudRoot);
        BuildStatusPanel(combatHudRoot);
        BuildRosterPanel(parent);
    }

    private void BuildCommandPanel(RectTransform parent)
    {
        RectTransform commandPanel = CreateHudSection("Command Panel", parent, 250f);
        CreateSectionTitle(commandPanel, "COMMAND");

        moveButton = CreateHudButton(commandPanel, "MOVE", "M", new Color(0.20f, 0.35f, 0.43f, 1f), RequestMoveAction);
        attackButton = CreateHudButton(commandPanel, "ATTACK", "A", new Color(0.43f, 0.17f, 0.12f, 1f), RequestAttackAction);
    }

    private void BuildActiveModuleStrip(RectTransform parent)
    {
        RectTransform modulePanel = CreateHudSection("Active Module Strip", parent, 0f);
        modulePanel.GetComponent<LayoutElement>().flexibleWidth = 1f;

        CreateSectionTitle(modulePanel, "ACTIVE MODULES");

        RectTransform row = CreatePanel("Active Module Cards", modulePanel, new Color(0f, 0f, 0f, 0f));
        row.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        activeWeaponSlot = BuildActiveModuleCard(row, "WEAPON", WeaponModuleColor);
        activeArmorSlot = BuildActiveModuleCard(row, "ARMOR", ArmorModuleColor);
        activeEngineSlot = BuildActiveModuleCard(row, "ENGINE", EngineModuleColor);
    }

    private void BuildStatusPanel(RectTransform parent)
    {
        RectTransform statusPanel = CreateHudSection("Turn Status Panel", parent, 390f);
        statusPanel.GetComponent<VerticalLayoutGroup>().spacing = 5;

        hudPlayerTitle = null;

        hudRound = CreateLabel(statusPanel, "ROUND -", 18f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        hudRound.characterSpacing = 1.5f;
        hudRound.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        BuildCurrentShipHpBar(statusPanel);

        hudPhase = null;
        hudMovement = null;
        hudAttack = null;
        hudArmor = null;
        hudLastAction = null;

        RectTransform buttonRow = CreatePanel("Turn Buttons", statusPanel, new Color(0f, 0f, 0f, 0f));
        buttonRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        var rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        cameraToggleButton = CreateSmallHudButton(buttonRow, "TOP", new Color(0.15f, 0.28f, 0.36f, 1f), ToggleCameraView);
        cameraToggleText = cameraToggleButton.GetComponentInChildren<TextMeshProUGUI>();
        rosterToggleButton = CreateSmallHudButton(buttonRow, "ROSTER", new Color(0.20f, 0.24f, 0.27f, 1f), ToggleRosterPanel);
        rosterToggleText = rosterToggleButton.GetComponentInChildren<TextMeshProUGUI>();
        endTurnButton = CreateSmallHudButton(buttonRow, "END TURN", new Color(0.54f, 0.20f, 0.13f, 1f), RequestEndTurn);
    }

    private void BuildRosterPanel(Transform parent)
    {
        rosterPanelRoot = CreatePanel("All Player Module Panel", parent, new Color(0.04f, 0.045f, 0.050f, 0.98f));
        rosterPanelRoot.anchorMin = new Vector2(1f, 0f);
        rosterPanelRoot.anchorMax = new Vector2(1f, 0f);
        rosterPanelRoot.pivot = new Vector2(1f, 0f);
        float rosterHeight = Mathf.Clamp(72f + ships.Count * 150f, 240f, 550f);
        rosterPanelRoot.sizeDelta = new Vector2(550f, rosterHeight);
        rosterPanelRoot.anchoredPosition = new Vector2(-18f, 252f);
        ApplyHudImage(rosterPanelRoot, hudSprites.Panel, Image.Type.Sliced, new Color(0.04f, 0.045f, 0.050f, 0.98f), true);
        AddHudOutline(rosterPanelRoot, new Color(0f, 0f, 0f, 0.72f), new Vector2(3f, -3f));

        var layout = rosterPanelRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 14);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateLabel(rosterPanelRoot, "ALL SHIPS", 20f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        playerPanels.Clear();
        foreach (Ship ship in ships)
        {
            if (ship == null)
                continue;

            playerPanels.Add(BuildPlayerPanel(rosterPanelRoot, ship));
        }

        rosterPanelRoot.gameObject.SetActive(false);
    }

    private void BuildVictoryOverlay(Transform parent)
    {
        victoryOverlayRoot = CreatePanel("Victory Overlay", parent, new Color(0f, 0f, 0f, 0.68f));
        Stretch(victoryOverlayRoot);
        victoryOverlayRoot.GetComponent<Image>().raycastTarget = true;

        RectTransform modal = CreatePanel("Victory Modal", victoryOverlayRoot, new Color(0.045f, 0.047f, 0.050f, 0.98f));
        modal.anchorMin = new Vector2(0.5f, 0.5f);
        modal.anchorMax = new Vector2(0.5f, 0.5f);
        modal.pivot = new Vector2(0.5f, 0.5f);
        modal.sizeDelta = new Vector2(560f, 320f);
        modal.anchoredPosition = Vector2.zero;
        ApplyHudImage(modal, hudSprites.Panel, Image.Type.Sliced, new Color(0.055f, 0.052f, 0.050f, 0.98f), true);
        AddHudOutline(modal, new Color(0f, 0f, 0f, 0.80f), new Vector2(3f, -3f));

        var layout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 28, 26);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI label = CreateLabel(modal, "MATCH COMPLETE", 18f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Center);
        label.characterSpacing = 2f;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        victoryTitle = CreateLabel(modal, "Player won the match!", 42f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        victoryTitle.fontSizeMin = 24f;
        victoryTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;

        victorySubtitle = CreateLabel(modal, "Final round complete", 18f, FontStyles.Bold, MutedTextColor, TextAlignmentOptions.Center);
        victorySubtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform buttonRow = CreatePanel("Victory Button Row", modal, new Color(0f, 0f, 0f, 0f));
        buttonRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

        var buttonLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 12;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = true;

        CreateSmallHudButton(buttonRow, "RESET MATCH", new Color(0.20f, 0.35f, 0.43f, 1f), ResetMatch);
        CreateSmallHudButton(buttonRow, "MAIN MENU", new Color(0.54f, 0.20f, 0.13f, 1f), ReturnToMainMenu);

        victoryOverlayRoot.gameObject.SetActive(false);
    }

    private void BuildCurrentShipHpBar(RectTransform parent)
    {
        RectTransform hpRoot = CreatePanel("Current Ship HP Bar", parent, new Color(0.025f, 0.070f, 0.035f, 1f));
        hpRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        AddHudOutline(hpRoot, new Color(0f, 0f, 0f, 0.72f), new Vector2(2f, -2f));

        hudHpLostFill = CreatePanel("Current Ship HP Lost Fill", hpRoot, HpLostColor);
        hudHpLostFill.anchorMin = Vector2.zero;
        hudHpLostFill.anchorMax = Vector2.one;
        hudHpLostFill.offsetMin = new Vector2(3f, 3f);
        hudHpLostFill.offsetMax = new Vector2(-3f, -3f);
        hudHpLostFill.gameObject.SetActive(false);

        hudHpFill = CreatePanel("Current Ship HP Fill", hpRoot, HpHealthyColor);
        hudHpFill.anchorMin = Vector2.zero;
        hudHpFill.anchorMax = Vector2.one;
        hudHpFill.offsetMin = new Vector2(3f, 3f);
        hudHpFill.offsetMax = new Vector2(-3f, -3f);

        hudHpDividers.Clear();
        hudHpDividers.AddRange(BuildHpDividers(hpRoot, 2));

        hudHpText = CreateLabel(hpRoot, "HP 0/0", 16f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
        hudHpText.margin = new Vector4(12f, 0f, 12f, 0f);
        Stretch(hudHpText.rectTransform);
    }

    private TextMeshProUGUI CreateInfoLine(RectTransform parent, string text)
    {
        TextMeshProUGUI label = CreateLabel(parent, text, 10f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        label.richText = true;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 11f;
        return label;
    }

    private RectTransform CreateHudSection(string name, RectTransform parent, float width)
    {
        RectTransform section = CreatePanel(name, parent, HeaderColor);
        ApplyHudImage(section, hudSprites.Panel, Image.Type.Sliced, HeaderColor, true);
        AddHudOutline(section, new Color(0f, 0f, 0f, 0.55f), new Vector2(2f, -2f));

        var layoutElement = section.gameObject.AddComponent<LayoutElement>();
        if (width > 0f)
        {
            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
            layoutElement.flexibleWidth = 0f;
        }
        else
        {
            layoutElement.flexibleWidth = 1f;
        }

        var layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 7);
        layout.spacing = 3;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return section;
    }

    private TextMeshProUGUI CreateSectionTitle(RectTransform parent, string text)
    {
        TextMeshProUGUI title = CreateLabel(parent, text, 12f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        title.characterSpacing = 2f;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        return title;
    }

    private Button CreateHudButton(RectTransform parent, string label, string hotkey, Color color, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRoot = CreatePanel(label + " Button", parent, color);
        buttonRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        ApplyHudImage(buttonRoot, hudSprites.Button, Image.Type.Sliced, color, true);
        AddHudOutline(buttonRoot, new Color(0f, 0f, 0f, 0.6f), new Vector2(2f, -2f));

        Button button = buttonRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRoot.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors(color);
        button.onClick.AddListener(onClick);

        var layout = buttonRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 12, 5, 5);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        RectTransform icon = CreatePanel("Button Icon", buttonRoot, new Color(0f, 0f, 0f, 0.28f));
        icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 32f;
        ApplyHudImage(icon, hudSprites.Badge, Image.Type.Sliced, new Color(0.92f, 0.82f, 0.60f, 0.22f), true);
        TextMeshProUGUI iconText = CreateLabel(icon, hotkey, 16f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(iconText.rectTransform);

        TextMeshProUGUI labelText = CreateLabel(buttonRoot, label, 20f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return button;
    }

    private Button CreateSmallHudButton(RectTransform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRoot = CreatePanel(label + " Button", parent, color);
        ApplyHudImage(buttonRoot, hudSprites.Button, Image.Type.Sliced, color, true);
        AddHudOutline(buttonRoot, new Color(0f, 0f, 0f, 0.55f), new Vector2(2f, -2f));

        Button button = buttonRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRoot.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors(color);
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonRoot, label, 13f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private ModuleSlotHud BuildActiveModuleCard(RectTransform parent, string label, Color activeColor)
    {
        RectTransform root = CreatePanel(label + " Card", parent, EmptyModuleColor);
        root.GetComponent<Image>().raycastTarget = true;
        ApplyHudImage(root, hudSprites.Card, Image.Type.Sliced, EmptyModuleColor, true);
        AddNeutralHudOutline(root, new Vector2(2f, -2f));

        LayoutElement slotLayout = root.gameObject.AddComponent<LayoutElement>();
        slotLayout.minWidth = 160f;
        slotLayout.preferredWidth = 196f;
        slotLayout.flexibleWidth = 1f;
        slotLayout.flexibleHeight = 1f;

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 2;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;

        TextMeshProUGUI header = CreateLabel(root, label, 11f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Center);
        header.characterSpacing = 1f;
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 13f;

        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI name = CreateLabel(root, "-", 15f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
        name.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        TextMeshProUGUI detail = CreateLabel(root, "-", 11f, FontStyles.Bold, MutedTextColor, TextAlignmentOptions.Center);
        detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;

        TextMeshProUGUI passive = CreateLabel(root, "-", 10f, FontStyles.Normal, TextColor, TextAlignmentOptions.Center);
        passive.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        var slot = new ModuleSlotHud
        {
            Root = root,
            Background = root.GetComponent<Image>(),
            ActiveColor = activeColor,
            Name = name,
            Detail = detail,
            Passive = passive
        };

        InstallModuleHover(slot);
        return slot;
    }

    private RectTransform CreateModuleRow(RectTransform parent)
    {
        RectTransform row = CreatePanel("Module Row", parent, new Color(0f, 0f, 0f, 0f));
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 58f;
        rowLayout.flexibleHeight = 0f;

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlHeight = true;
        layout.childAlignment = TextAnchor.UpperLeft;
        return row;
    }

    private PlayerHudPanel BuildPlayerPanel(RectTransform parent, Ship ship)
    {
        RectTransform panel = CreatePanel("Player Module Panel", parent, HeaderColor);
        ApplyHudImage(panel, hudSprites.Panel, Image.Type.Sliced, HeaderColor, true);
        AddHudOutline(panel, new Color(0f, 0f, 0f, 0.62f), new Vector2(2f, -2f));
        LayoutElement panelLayout = panel.gameObject.AddComponent<LayoutElement>();
        panelLayout.preferredHeight = 142f;
        panelLayout.flexibleHeight = 0f;

        var verticalLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(12, 12, 8, 8);
        verticalLayout.spacing = 6;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childControlHeight = true;

        RectTransform header = CreatePanel("Player Header", panel, new Color(0f, 0f, 0f, 0f));
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 6;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;

        int playerId = GetShipPlayerId(ship, playerPanels.Count);
        TextMeshProUGUI title = CreateLabel(header, IronTideGameState.GetPlayerDisplayName(playerId), 18f,
            FontStyles.Bold, IronTideGameState.GetPlayerColor(playerId, GoldColor), TextAlignmentOptions.Left);
        var titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 24f;
        titleLayout.flexibleWidth = 1f;
        title.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform hpRoot = CreatePanel("HP Bar", panel, new Color(0.055f, 0.095f, 0.050f, 1f));
        LayoutElement hpLayout = hpRoot.gameObject.AddComponent<LayoutElement>();
        hpLayout.minHeight = 28f;
        hpLayout.preferredHeight = 28f;
        AddHudOutline(hpRoot, new Color(0f, 0f, 0f, 0.72f), new Vector2(2f, -2f));

        RectTransform hpLostFill = CreatePanel("HP Lost Fill", hpRoot, HpLostColor);
        hpLostFill.anchorMin = Vector2.zero;
        hpLostFill.anchorMax = Vector2.one;
        hpLostFill.offsetMin = new Vector2(3f, 3f);
        hpLostFill.offsetMax = new Vector2(-3f, -3f);
        hpLostFill.gameObject.SetActive(false);

        RectTransform hpFill = CreatePanel("HP Fill", hpRoot, HpHealthyColor);
        hpFill.anchorMin = Vector2.zero;
        hpFill.anchorMax = Vector2.one;
        hpFill.offsetMin = new Vector2(3f, 3f);
        hpFill.offsetMax = new Vector2(-3f, -3f);

        List<RectTransform> hpDividers = BuildHpDividers(hpRoot, 2);

        TextMeshProUGUI hpText = CreateLabel(hpRoot, "HP 10 / 10", 16f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
        hpText.overflowMode = TextOverflowModes.Ellipsis;
        hpText.fontSizeMin = 10f;
        hpText.margin = new Vector4(12f, 0f, 12f, 0f);
        Stretch(hpText.rectTransform);

        RectTransform cards = CreateModuleRow(panel);

        return new PlayerHudPanel
        {
            Ship = ship,
            Title = title,
            HpLostFill = hpLostFill,
            HpFill = hpFill,
            HpDividers = hpDividers,
            HpText = hpText,
            WeaponSlot = BuildModuleSlot(cards, "WEAPON", WeaponModuleColor),
            ArmorSlot = BuildModuleSlot(cards, "ARMOR", ArmorModuleColor),
            EngineSlot = BuildModuleSlot(cards, "ENGINE", EngineModuleColor)
        };
    }

    private List<RectTransform> BuildHpDividers(RectTransform hpRoot, int count)
    {
        var dividers = new List<RectTransform>(count);
        for (int i = 0; i < count; i++)
        {
            RectTransform divider = CreatePanel("HP Divider", hpRoot, HpDividerColor);
            divider.anchorMin = new Vector2(0.5f, 0f);
            divider.anchorMax = new Vector2(0.5f, 1f);
            divider.pivot = new Vector2(0.5f, 0.5f);
            divider.sizeDelta = new Vector2(3f, 0f);
            divider.anchoredPosition = Vector2.zero;
            divider.gameObject.SetActive(false);
            dividers.Add(divider);
        }

        return dividers;
    }

    private ModuleSlotHud BuildModuleSlot(RectTransform parent, string label, Color activeColor)
    {
        RectTransform root = CreatePanel(label, parent, EmptyModuleColor);
        LayoutElement slotLayout = root.gameObject.AddComponent<LayoutElement>();
        slotLayout.minHeight = 58f;
        slotLayout.preferredHeight = 58f;
        slotLayout.flexibleHeight = 0f;
        root.GetComponent<Image>().raycastTarget = true;
        ApplyHudImage(root, hudSprites.Card, Image.Type.Sliced, EmptyModuleColor, true);
        AddNeutralHudOutline(root, new Vector2(1f, -1f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 2;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;

        TextMeshProUGUI header = CreateLabel(root, label, 9f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        header.characterSpacing = 1f;
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 11f;

        TextMeshProUGUI name = CreateLabel(root, "-", 14f, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        name.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.fontSizeMin = 9f;

        TextMeshProUGUI detail = CreateLabel(root, "-", 10f, FontStyles.Bold, MutedTextColor, TextAlignmentOptions.Left);
        detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        detail.overflowMode = TextOverflowModes.Ellipsis;
        detail.fontSizeMin = 8f;

        var slot = new ModuleSlotHud
        {
            Root = root,
            Background = root.GetComponent<Image>(),
            ActiveColor = activeColor,
            Name = name,
            Detail = detail
        };

        InstallModuleHover(slot);
        return slot;
    }

    private void InstallModuleHover(ModuleSlotHud slot)
    {
        var trigger = slot.Root.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowModuleTooltip(slot));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideModuleTooltip(slot));
        trigger.triggers.Add(exit);
    }

    private void BuildModuleTooltip(Transform parent)
    {
        moduleTooltip = CreatePanel("Module Tooltip", parent, TooltipColor);
        moduleTooltip.anchorMin = new Vector2(0.5f, 0f);
        moduleTooltip.anchorMax = new Vector2(0.5f, 0f);
        moduleTooltip.pivot = new Vector2(0.5f, 0f);
        moduleTooltip.sizeDelta = new Vector2(520f, 238f);
        moduleTooltip.anchoredPosition = new Vector2(0f, 178f);
        ApplyHudImage(moduleTooltip, hudSprites.Panel, Image.Type.Sliced, TooltipColor, true);
        AddHudOutline(moduleTooltip, new Color(0f, 0f, 0f, 0.72f), new Vector2(3f, -3f));

        var layout = moduleTooltip.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 6;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        tooltipTitle = CreateLabel(moduleTooltip, "Module", 24f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        tooltipTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        tooltipMeta = CreateLabel(moduleTooltip, "-", 16f, FontStyles.Bold, MutedTextColor, TextAlignmentOptions.Left);
        tooltipMeta.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        tooltipStats = CreateLabel(moduleTooltip, "-", 16f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        tooltipStats.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        tooltipPassive = CreateLabel(moduleTooltip, "-", 15f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        tooltipPassive.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;

        moduleTooltip.gameObject.SetActive(false);
    }

    private void ShowModuleTooltip(ModuleSlotHud slot)
    {
        if (slot == null || slot.Card == null || !slot.Card.IsValid || moduleTooltip == null)
            return;

        hoveredSlot = slot;
        IronTideModuleCardEntry card = slot.Card;
        tooltipTitle.SetText(card.DisplayName);
        tooltipMeta.SetText($"{card.TierLabel} | {card.ArchetypeLabel} | {GetModuleBonusLabel(card)} | {(slot.ModuleEnabled ? "Active" : "Damaged")}");
        tooltipStats.SetText(GetTooltipStats(card));

        if (card.HasPassive)
            tooltipPassive.SetText($"Passive - {card.PassiveName}: {card.PassiveDescription}");
        else
            tooltipPassive.SetText($"Rules: {card.BaseRulesText}");

        moduleTooltip.gameObject.SetActive(true);
    }

    private void HideModuleTooltip(ModuleSlotHud slot)
    {
        if (hoveredSlot != slot)
            return;

        hoveredSlot = null;
        if (moduleTooltip != null)
            moduleTooltip.gameObject.SetActive(false);
    }

    private void RefreshCombatHud()
    {
        Ship currentShip = GetCurrentShip();
        if (currentShip == null || currentShip.shipInfo == null)
            return;

        int playerId = GetShipPlayerId(currentShip, 0);
        if (hudPlayerTitle != null)
        {
            hudPlayerTitle.SetText($"{IronTideGameState.GetPlayerDisplayName(playerId)}'s Turn");
            hudPlayerTitle.color = IronTideGameState.GetPlayerColor(playerId, GoldColor);
        }

        if (hudPhase != null)
            hudPhase.SetText(FormatInfoLine("PHASE", GetPhaseLabel(), GoldHex));
        if (hudRound != null)
            hudRound.SetText($"ROUND {IronTideGameState.CombatRound} / {IronTideGameState.MaxCombatRounds}");
        if (hudMovement != null)
            hudMovement.SetText(FormatInfoLine("MOVE", $"{currentShip.shipMovement.avaliableTileDistance} tiles left", MoveHex));
        if (hudAttack != null)
            hudAttack.SetText(FormatInfoLine("ATTACK", GetAttackLabel(currentShip), GoldHex));
        if (hudArmor != null)
            hudArmor.SetText(FormatInfoLine("ARMOR", currentShip.shipInfo.GetArmor().ToString(), MutedHex));
        if (hudLastAction != null)
            hudLastAction.SetText(FormatInfoLine("LAST", lastActionText, GoldHex));

        RefreshCurrentShipHpBar(currentShip.shipInfo);
        RefreshModuleSlot(activeWeaponSlot, currentShip.shipInfo.WeaponModule, currentShip.shipInfo.WeaponEnabled);
        RefreshModuleSlot(activeArmorSlot, currentShip.shipInfo.ArmorModule, currentShip.shipInfo.ArmorEnabled);
        RefreshModuleSlot(activeEngineSlot, currentShip.shipInfo.EngineModule, currentShip.shipInfo.EngineEnabled);
        RefreshCommandButtons(currentShip);
    }

    private Ship GetCurrentShip()
    {
        if (TurnManager.Instance == null)
            return ships.Count > 0 ? ships[0] : null;

        TurnPlayerController currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        if (currentPlayer == null)
            return null;

        return currentPlayer.GetComponent<Ship>();
    }

    private string GetPhaseLabel()
    {
        if (TurnManager.Instance == null)
            return "-";

        TurnPlayerController currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        if (currentPlayer != null && currentPlayer.IsResolvingAttackResult)
            return "Attack result";

        if (currentPlayer != null && !currentPlayer.CanReceiveTurnInput)
            return "Camera focusing";

        switch (TurnManager.Instance.currentPhase)
        {
            case TurnPhase.RollMovement:
                return "Choose Move [M] or Attack [A]";
            case TurnPhase.Move:
                return "Move";
            case TurnPhase.RollAttack:
                return "Choose Move [M] or Attack [A]";
            case TurnPhase.Attack:
                return "Attack [click target]";
            default:
                return "-";
        }
    }

    private void RefreshCommandButtons(Ship currentShip)
    {
        if (TurnManager.Instance == null || currentShip == null || currentShip.turnPlayerController == null)
            return;

        TurnPhase phase = TurnManager.Instance.currentPhase;
        bool isCurrentTurn = currentShip.turnPlayerController.CanReceiveTurnInput;

        SetButtonInteractable(moveButton, isCurrentTurn &&
            (phase == TurnPhase.RollMovement || phase == TurnPhase.Move || phase == TurnPhase.RollAttack));
        SetButtonInteractable(attackButton, isCurrentTurn &&
            (phase == TurnPhase.RollMovement || phase == TurnPhase.RollAttack));
        SetButtonInteractable(endTurnButton, isCurrentTurn &&
            (phase == TurnPhase.RollMovement || phase == TurnPhase.Move || phase == TurnPhase.RollAttack || phase == TurnPhase.Attack));
        SetButtonInteractable(rosterToggleButton, true);
        SetButtonInteractable(cameraToggleButton, GetCameraController() != null);

        if (rosterToggleText != null)
            rosterToggleText.SetText(rosterOpen ? "HIDE" : "ROSTER");

        RefreshCameraToggleText();
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private void RequestMoveAction()
    {
        GetCurrentShip()?.turnPlayerController?.RequestMoveAction();
    }

    private void RequestAttackAction()
    {
        GetCurrentShip()?.turnPlayerController?.RequestAttackAction();
    }

    private void RequestEndTurn()
    {
        GetCurrentShip()?.turnPlayerController?.RequestEndCurrentAction();
    }

    private void ToggleCameraView()
    {
        CameraController controller = GetCameraController();
        if (controller == null)
            return;

        controller.ToggleStrategyView();
        RefreshCameraToggleText();
    }

    private void RefreshCameraToggleText()
    {
        if (cameraToggleText == null)
            return;

        CameraController controller = GetCameraController();
        cameraToggleText.SetText(controller != null && controller.IsStrategyTopDownActive ? "3RD" : "TOP");
    }

    private CameraController GetCameraController()
    {
        if (cachedCameraController != null)
            return cachedCameraController;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            cachedCameraController = mainCamera.GetComponent<CameraController>();

        if (cachedCameraController == null)
            cachedCameraController = FindFirstObjectByType<CameraController>();

        return cachedCameraController;
    }

    private void ToggleRosterPanel()
    {
        rosterOpen = !rosterOpen;
        if (rosterPanelRoot != null)
            rosterPanelRoot.gameObject.SetActive(rosterOpen);

        if (rosterToggleText != null)
            rosterToggleText.SetText(rosterOpen ? "HIDE" : "ROSTER");
    }

    private void RefreshAllModulePanels()
    {
        foreach (PlayerHudPanel panel in playerPanels)
            RefreshPlayerPanel(panel);
    }

    private void RefreshPlayerPanel(PlayerHudPanel panel)
    {
        if (panel == null || panel.Ship == null || panel.Ship.shipInfo == null)
            return;

        ShipInfo info = panel.Ship.shipInfo;
        int playerId = GetShipPlayerId(panel.Ship, playerPanels.IndexOf(panel));
        if (panel.Title != null)
        {
            panel.Title.SetText(IronTideGameState.GetPlayerDisplayName(playerId));
            panel.Title.color = IronTideGameState.GetPlayerColor(playerId, GoldColor);
        }

        int displayMaxHealth = GetRosterDisplayMaxHealth(info);
        int displayHealth = Mathf.Clamp(info.Health, 0, displayMaxHealth);
        float hpRatio = displayMaxHealth > 0 ? Mathf.Clamp01((float)displayHealth / displayMaxHealth) : 0f;
        if (panel.HpLostFill != null)
        {
            bool showLostCapacity = GetEquippedModuleAmount(info) > 1 && displayHealth < displayMaxHealth;
            panel.HpLostFill.gameObject.SetActive(showLostCapacity);
        }

        panel.HpFill.anchorMax = new Vector2(hpRatio, 1f);
        panel.HpFill.offsetMin = new Vector2(3f, 3f);
        panel.HpFill.offsetMax = new Vector2(-3f, -3f);
        panel.HpFill.GetComponent<Image>().color = GetHpColor(hpRatio);
        panel.HpText.SetText($"HP {displayHealth} / {displayMaxHealth}");
        RefreshHpDividers(panel, displayMaxHealth);

        RefreshModuleSlot(panel.WeaponSlot, info.WeaponModule, info.WeaponEnabled);
        RefreshModuleSlot(panel.ArmorSlot, info.ArmorModule, info.ArmorEnabled);
        RefreshModuleSlot(panel.EngineSlot, info.EngineModule, info.EngineEnabled);
    }

    private void RefreshHpDividers(PlayerHudPanel panel, int maxHealth)
    {
        RefreshHpDividers(panel.HpDividers, maxHealth);
    }

    private static int GetRosterDisplayMaxHealth(ShipInfo info)
    {
        if (info == null)
            return 0;

        int equippedModules = GetEquippedModuleAmount(info);
        if (equippedModules > 0)
            return equippedModules * ShipInfo.HealthPerModule;

        return Mathf.Max(0, info.MaxHealth);
    }

    private static int GetEquippedModuleAmount(ShipInfo info)
    {
        if (info == null)
            return 0;

        int amount = 0;
        if (info.WeaponModule != null && info.WeaponModule.IsValid)
            amount++;
        if (info.ArmorModule != null && info.ArmorModule.IsValid)
            amount++;
        if (info.EngineModule != null && info.EngineModule.IsValid)
            amount++;

        return amount;
    }

    private void RefreshHpDividers(List<RectTransform> dividers, int maxHealth)
    {
        if (dividers == null)
            return;

        int dividerCount = Mathf.Max(0, (maxHealth / ShipInfo.HealthPerModule) - 1);
        for (int i = 0; i < dividers.Count; i++)
        {
            RectTransform divider = dividers[i];
            bool show = i < dividerCount && maxHealth > 0;
            divider.gameObject.SetActive(show);

            if (!show)
                continue;

            float x = (float)((i + 1) * ShipInfo.HealthPerModule) / maxHealth;
            divider.anchorMin = new Vector2(x, 0f);
            divider.anchorMax = new Vector2(x, 1f);
            divider.anchoredPosition = Vector2.zero;
        }
    }

    private void RefreshCurrentShipHpBar(ShipInfo info)
    {
        if (info == null || hudHpFill == null || hudHpText == null)
            return;

        int displayMaxHealth = GetRosterDisplayMaxHealth(info);
        int displayHealth = Mathf.Clamp(info.Health, 0, displayMaxHealth);
        float hpRatio = displayMaxHealth > 0 ? Mathf.Clamp01((float)displayHealth / displayMaxHealth) : 0f;
        if (hudHpLostFill != null)
        {
            bool showLostCapacity = GetEquippedModuleAmount(info) > 1 && displayHealth < displayMaxHealth;
            hudHpLostFill.gameObject.SetActive(showLostCapacity);
        }

        hudHpFill.anchorMax = new Vector2(hpRatio, 1f);
        hudHpFill.offsetMin = new Vector2(3f, 3f);
        hudHpFill.offsetMax = new Vector2(-3f, -3f);
        hudHpFill.GetComponent<Image>().color = GetHpColor(hpRatio);

        hudHpText.SetText($"HP {displayHealth} / {displayMaxHealth}");
        RefreshHpDividers(hudHpDividers, displayMaxHealth);
    }

    private static Color GetHpColor(float hpRatio)
    {
        if (hpRatio <= 0.3f)
            return new Color(0.94f, 0.24f, 0.18f, 1f);

        if (hpRatio <= 0.6f)
            return new Color(0.95f, 0.66f, 0.18f, 1f);

        return HpHealthyColor;
    }

    private void RefreshModuleSlot(ModuleSlotHud slot, IronTideModuleCardEntry card, bool enabled)
    {
        if (slot == null)
            return;

        slot.Card = card;
        slot.ModuleEnabled = enabled;

        if (card == null || !card.IsValid)
        {
            slot.Background.color = EmptyModuleColor;
            slot.Name.SetText("Empty");
            slot.Detail.SetText("No module");
            if (slot.Passive != null)
                slot.Passive.SetText("No module equipped");
            if (slot.Icon != null)
                slot.Icon.SetText(GetSlotIcon(slot.Root.name));

            if (hoveredSlot == slot)
                HideModuleTooltip(slot);

            return;
        }

        slot.Background.color = enabled ? ActiveModuleColor : BrokenModuleColor;
        slot.Name.SetText(card.DisplayName);
        slot.Detail.SetText(enabled ? GetModuleBonusLabel(card) : "Damaged");
        if (slot.Passive != null)
            slot.Passive.SetText(enabled ? GetModuleCardFooter(card) : "Module offline");
        if (slot.Icon != null)
            slot.Icon.SetText(GetSlotIcon(card.SlotType));

        if (hoveredSlot == slot)
            ShowModuleTooltip(slot);
    }

    private static string GetModuleBonusLabel(IronTideModuleCardEntry card)
    {
        if (card == null || !card.IsValid)
            return string.Empty;

        string modifier = card.ModifierLabel;
        switch (card.SlotType)
        {
            case BasicModuleType.Armor:
                return $"{modifier} armor";
            case BasicModuleType.Engine:
                string tileLabel = Mathf.Abs(card.BaseModifier) == 1 ? "tile" : "tiles";
                return $"{modifier} {tileLabel}";
            default:
                return $"{modifier} base | {GetCompactRangeLabel(card)}";
        }
    }

    private static string GetCompactRangeLabel(IronTideModuleCardEntry card)
    {
        if (card == null)
            return string.Empty;

        switch (card.Archetype)
        {
            case IronTideModuleArchetype.LongRangeWeapon:
                return "R1-4 +0, R5 +1, R6 +2";
            case IronTideModuleArchetype.MediumRangeWeapon:
                return "R1-4 +0";
            case IronTideModuleArchetype.ShortRangeWeapon:
                return "R1 +2, R2 +0, R3 -1, R4 -2";
            default:
                return card.ModifierRulesSummary;
        }
    }

    private static string GetModuleCardFooter(IronTideModuleCardEntry card)
    {
        if (card == null || !card.IsValid)
            return string.Empty;

        if (card.UsesDice)
            return $"{card.DiceCount}xD{card.DiceSides} | {card.TierLabel} | {card.HeaderLabel}";

        if (card.HasPassive)
            return $"Passive | {card.TierLabel}";

        return card.TierLabel;
    }

    private static string GetSlotIcon(BasicModuleType slotType)
    {
        switch (slotType)
        {
            case BasicModuleType.Weapon:
                return "W";
            case BasicModuleType.Armor:
                return "A";
            case BasicModuleType.Engine:
                return "E";
            default:
                return "M";
        }
    }

    private static string GetSlotIcon(string label)
    {
        if (label.Contains("ARMOR"))
            return "A";
        if (label.Contains("ENGINE"))
            return "E";
        return "W";
    }

    private void UpdateWinState()
    {
        if (gameOver)
            return;

        int aliveCount = 0;
        int winner = 0;
        foreach (Ship ship in ships)
        {
            if (ship == null || ship.shipInfo == null || ship.shipInfo.Sunk)
                continue;

            aliveCount++;
            winner = ship.turnPlayerController != null ? ship.turnPlayerController.playerID + 1 : winner;
        }

        if (aliveCount == 1 && ships.Count > 1)
        {
            gameOver = true;
            winnerNumber = winner;
            if (winIndicator != null)
            {
                string winnerName = IronTideGameState.GetPlayerDisplayName(winnerNumber - 1);
                string resultText = IronTideGameState.IsFinalCombatRound
                    ? $"{winnerName} won the match!"
                    : $"{winnerName} won round {IronTideGameState.CombatRound}!";
                winIndicator.SetText(resultText);
                winIndicator.gameObject.SetActive(true);
            }

            FinishRound(winnerNumber - 1);
        }
    }

    private void FinishRound(int winnerPlayerId)
    {
        if (transitionStarted)
            return;

        transitionStarted = true;
        IronTideGameState.SaveLoadouts(ships);
        DisableCombatInput();

        if (!IronTideGameState.ShouldOpenShopAfterCombat)
        {
            IronTideGameState.CompleteFinalRound(winnerPlayerId);
            ShowVictoryOverlay(winnerPlayerId);
            return;
        }

        IronTideGameState.AwardShopGold(winnerPlayerId);
        StartCoroutine(LoadShoppingAfterRoundDelay());
    }

    private void ShowVictoryOverlay(int winnerPlayerId)
    {
        if (victoryOverlayRoot == null)
            return;

        string winnerName = IronTideGameState.GetPlayerDisplayName(winnerPlayerId);
        Color winnerColor = IronTideGameState.GetPlayerColor(winnerPlayerId, GoldColor);

        if (victoryTitle != null)
        {
            victoryTitle.SetText($"{winnerName} won the match!");
            victoryTitle.color = winnerColor;
        }

        if (victorySubtitle != null)
            victorySubtitle.SetText($"Round {IronTideGameState.CombatRound} complete");

        if (rosterPanelRoot != null)
            rosterPanelRoot.gameObject.SetActive(false);
        rosterOpen = false;

        if (moduleTooltip != null)
            moduleTooltip.gameObject.SetActive(false);
        hoveredSlot = null;

        victoryOverlayRoot.gameObject.SetActive(true);
        victoryOverlayRoot.SetAsLastSibling();
    }

    private void ResetMatch()
    {
        Time.timeScale = 1f;
        IronTideGameState.ResetAll();
        SceneManager.LoadScene(IronTideGameState.CombatSceneName);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        IronTideGameState.ResetAll();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private IEnumerator LoadShoppingAfterRoundDelay()
    {
        if (roundTransitionDelay > 0f)
            yield return new WaitForSeconds(roundTransitionDelay);

        SceneManager.LoadScene(IronTideGameState.ShoppingSceneName);
    }

    private void DisableCombatInput()
    {
        foreach (Ship ship in ships)
        {
            if (ship != null && ship.turnPlayerController != null)
                ship.turnPlayerController.SetMyTurn(false);
        }
    }

    private void HandleTurnStarted(int playerId)
    {
        lastActionText = "New turn started.";
        attackText = string.IsNullOrWhiteSpace(previousAttackText)
            ? "Attack not rolled."
            : previousAttackText;
    }

    private void HandleMovementRolled(int total)
    {
        lastActionText = $"Movement rolled: {total} tiles.";
    }

    private void HandleAttackRolled(int total)
    {
        lastActionText = $"Attack total: {total}.";
    }

    private void HandleDamageDealt(int damage)
    {
        lastActionText = $"Damage dealt after armor: {damage}.";
    }

    private void HandleTurnFeedback(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            lastActionText = message;
    }

    private void HandleAttackResolved(int diceTotal, int bonusTotal, int damageReduction, int damageDealt)
    {
        attackText = $"Roll {diceTotal} {FormatSignedNumber(bonusTotal)} | Enemy armor {damageReduction} | Damage {damageDealt}";
        previousAttackText = attackText;
        lastActionText = attackText;
    }

    private void HandleAttackPrepared(int diceTotal, int bonusTotal)
    {
        attackText = $"Roll {diceTotal} {FormatSignedNumber(bonusTotal)} | choose target";
        lastActionText = attackText;
    }

    private static string GetCardName(IronTideModuleCardEntry card)
    {
        return card != null && card.IsValid ? card.DisplayName : "None";
    }

    private static string GetTooltipStats(IronTideModuleCardEntry card)
    {
        string dice = card.UsesDice ? $"{card.DiceCount}xD{card.DiceSides}" : "No dice";
        string economy = $"Buy {card.BuyCost}g | Sell {card.SellValue}g";

        if (card.SlotType == BasicModuleType.Weapon)
        {
            string damage = GetCardDamageRangeLabel(card);
            return $"Damage {damage}\nDice {dice} | Base {card.ModifierLabel}\n{card.ModifierRulesSummary} | {economy}";
        }

        if (card.SlotType == BasicModuleType.Armor)
            return $"Armor {card.ModifierLabel}\n{card.ModifierRulesSummary}\nEach active module adds +10 HP | {economy}";

        string tileWord = Mathf.Abs(card.BaseModifier) == 1 ? "tile" : "tiles";
        return $"Movement {card.ModifierLabel} {tileWord}\n{card.ModifierRulesSummary}\nMove roll {dice} + engine bonus | {economy}";
    }

    private static string GetCardDamageRangeLabel(IronTideModuleCardEntry card)
    {
        if (card == null || !card.IsValid || !card.UsesDice)
            return "1-6";

        int minDamage = card.DiceCount + card.BaseModifier;
        int maxDamage = card.DiceCount * card.DiceSides + card.BaseModifier;

        if (card.PassiveKey == "perfect_shot_t1" ||
            card.PassiveKey == "perfect_shot_t2" ||
            card.PassiveKey == "lucky_legendary")
        {
            maxDamage += card.DiceSides;
        }

        minDamage = Mathf.Max(0, minDamage);
        maxDamage = Mathf.Max(minDamage, maxDamage);

        return minDamage == maxDamage ? minDamage.ToString() : $"{minDamage}-{maxDamage}";
    }

    private string GetAttackLabel(Ship ship)
    {
        if (ship == null || ship.shipInfo == null)
            return "-";

        if (!string.IsNullOrWhiteSpace(attackText) && attackText != "Attack not rolled.")
            return attackText;

        if (TurnManager.Instance != null && TurnManager.Instance.currentPhase == TurnPhase.Attack && ship.shipWeapon != null)
        {
            int targetCount = ship.shipWeapon.ReachableTargetsDamageModifiers.Count;
            string targetText = targetCount == 1 ? "1 target" : $"{targetCount} targets";
            return $"Choose target, then roll | {targetText}";
        }

        return "Press A, choose target, then roll.";
    }

    private static string FormatInfoLine(string label, string value, string valueColor)
    {
        return $"<color={GoldHex}><b>{label}</b></color>  <color={valueColor}>{value}</color>";
    }

    private static string FormatSignedNumber(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static ColorBlock CreateButtonColors(Color baseColor)
    {
        return new ColorBlock
        {
            normalColor = baseColor,
            highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f),
            pressedColor = Color.Lerp(baseColor, Color.black, 0.18f),
            selectedColor = Color.Lerp(baseColor, Color.white, 0.10f),
            disabledColor = new Color(baseColor.r * 0.42f, baseColor.g * 0.42f, baseColor.b * 0.42f, 0.46f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    private static void ApplyHudImage(RectTransform rect, Sprite sprite, Image.Type type, Color color, bool raycastTarget)
    {
        if (rect == null)
            return;

        Image image = rect.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = sprite;
        image.type = type;
        image.color = color;
        image.raycastTarget = raycastTarget;
    }

    private static void AddHudOutline(RectTransform rect, Color color, Vector2 distance)
    {
        if (rect == null)
            return;

        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.58f, 0.34f, 0.36f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
    }

    private static void AddNeutralHudOutline(RectTransform rect, Vector2 distance)
    {
        if (rect == null)
            return;

        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
        shadow.effectDistance = distance;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var rect = panelObject.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        var image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string text, float fontSize,
        FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Min(12f, fontSize);
        label.fontSizeMax = fontSize;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static int GetShipPlayerId(Ship ship, int fallbackPlayerId)
    {
        if (ship != null && ship.turnPlayerController != null)
            return ship.turnPlayerController.playerID;

        return fallbackPlayerId;
    }

    // Rebuild UI 
    public void RebuildUI()
    {
        playerPanels.Clear();

        Transform oldSidebar = transform.Find("Playtest Sidebar");
        Transform oldHud = transform.Find("Combat Command HUD");
        Transform oldRoster = transform.Find("All Player Module Panel");
        Transform oldTooltip = transform.Find("Module Tooltip");
        Transform oldVictory = transform.Find("Victory Overlay");

        if (oldSidebar != null)
        {
            DestroyImmediate(oldSidebar.gameObject);
        }
        if (oldHud != null)
        {
            DestroyImmediate(oldHud.gameObject);
        }
        if (oldRoster != null)
        {
            DestroyImmediate(oldRoster.gameObject);
        }
        if (oldTooltip != null)
        {
            DestroyImmediate(oldTooltip.gameObject);
        }
        if (oldVictory != null)
        {
            DestroyImmediate(oldVictory.gameObject);
        }

        ResolveShipReferences();

        SetupPlayerModules();

        rosterOpen = false;
        hudSprites = HudSprites.Create();
        BuildCombatHud(transform);
        BuildModuleTooltip(transform);
        BuildVictoryOverlay(transform);

        RefreshAllModulePanels();
    }

    private sealed class HudSprites
    {
        public Sprite Panel;
        public Sprite Button;
        public Sprite Card;
        public Sprite Badge;

        public static HudSprites Create()
        {
            return new HudSprites
            {
                Panel = CreateFramedSprite("IronTide HUD Panel", new Color(0.11f, 0.095f, 0.082f, 1f),
                    new Color(0.70f, 0.52f, 0.32f, 1f), new Color(0.02f, 0.018f, 0.016f, 1f), 5),
                Button = CreateFramedSprite("IronTide HUD Button", new Color(0.42f, 0.19f, 0.13f, 1f),
                    new Color(0.93f, 0.72f, 0.45f, 1f), new Color(0.07f, 0.035f, 0.025f, 1f), 4),
                Card = CreateFramedSprite("IronTide HUD Card", new Color(0.15f, 0.13f, 0.12f, 1f),
                    new Color(0.82f, 0.62f, 0.38f, 1f), new Color(0.025f, 0.024f, 0.024f, 1f), 5),
                Badge = CreateFramedSprite("IronTide HUD Badge", new Color(0.18f, 0.15f, 0.12f, 1f),
                    new Color(0.92f, 0.75f, 0.48f, 1f), new Color(0.03f, 0.028f, 0.024f, 1f), 3)
            };
        }

        private static Sprite CreateFramedSprite(string name, Color fill, Color edge, Color shadow, int border)
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    float verticalShade = Mathf.Lerp(0.15f, -0.10f, (float)y / (size - 1));
                    Color pixel = Adjust(fill, verticalShade);

                    if (minEdge < border)
                        pixel = minEdge < 2 ? shadow : edge;
                    else if ((x + y) % 11 == 0)
                        pixel = Adjust(pixel, 0.025f);

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(14f, 14f, 14f, 14f));
        }

        private static Sprite CreateWeaponArtSprite()
        {
            Texture2D texture = CreateArtTexture("IronTide Weapon Art Texture",
                new Color(0.13f, 0.07f, 0.055f, 1f), new Color(0.035f, 0.045f, 0.060f, 1f));

            PaintCircle(texture, new Vector2(28f, 30f), 22f, new Color(0.95f, 0.23f, 0.09f, 0.48f));
            PaintCircle(texture, new Vector2(28f, 30f), 13f, new Color(1f, 0.77f, 0.20f, 0.82f));
            PaintCircle(texture, new Vector2(25f, 33f), 6f, new Color(1f, 0.94f, 0.62f, 0.95f));
            PaintLine(texture, new Vector2(34f, 31f), new Vector2(78f, 15f), 8f, new Color(0.94f, 0.96f, 0.94f, 0.94f));
            PaintLine(texture, new Vector2(37f, 34f), new Vector2(80f, 18f), 3f, new Color(0.54f, 0.36f, 0.25f, 0.88f));
            PaintLine(texture, new Vector2(18f, 18f), new Vector2(45f, 39f), 3f, new Color(1f, 0.64f, 0.16f, 0.72f));
            return FinishArtSprite(texture);
        }

        private static Sprite CreateArmorArtSprite()
        {
            Texture2D texture = CreateArtTexture("IronTide Armor Art Texture",
                new Color(0.060f, 0.115f, 0.170f, 1f), new Color(0.025f, 0.040f, 0.065f, 1f));

            PaintDiamond(texture, new Vector2(47f, 30f), 25f, 24f, new Color(0.90f, 0.92f, 0.86f, 0.95f));
            PaintDiamond(texture, new Vector2(47f, 30f), 18f, 17f, new Color(0.18f, 0.33f, 0.45f, 0.86f));
            PaintLine(texture, new Vector2(47f, 12f), new Vector2(47f, 47f), 3f, new Color(1f, 0.98f, 0.86f, 0.90f));
            PaintLine(texture, new Vector2(29f, 30f), new Vector2(65f, 30f), 3f, new Color(1f, 0.98f, 0.86f, 0.78f));
            PaintCircle(texture, new Vector2(30f, 17f), 6f, new Color(0.88f, 0.92f, 0.94f, 0.70f));
            PaintCircle(texture, new Vector2(66f, 42f), 6f, new Color(0.88f, 0.92f, 0.94f, 0.70f));
            return FinishArtSprite(texture);
        }

        private static Sprite CreateEngineArtSprite()
        {
            Texture2D texture = CreateArtTexture("IronTide Engine Art Texture",
                new Color(0.035f, 0.135f, 0.135f, 1f), new Color(0.020f, 0.045f, 0.060f, 1f));

            PaintLine(texture, new Vector2(11f, 42f), new Vector2(80f, 28f), 5f, new Color(0.22f, 0.84f, 0.96f, 0.64f));
            PaintLine(texture, new Vector2(15f, 34f), new Vector2(72f, 20f), 3f, new Color(0.72f, 0.93f, 1f, 0.82f));
            PaintLine(texture, new Vector2(18f, 48f), new Vector2(70f, 38f), 3f, new Color(0.22f, 0.50f, 0.80f, 0.72f));
            PaintCircle(texture, new Vector2(72f, 23f), 11f, new Color(0.86f, 0.95f, 1f, 0.70f));
            PaintLine(texture, new Vector2(72f, 23f), new Vector2(85f, 14f), 4f, new Color(0.90f, 0.96f, 1f, 0.88f));
            PaintLine(texture, new Vector2(72f, 23f), new Vector2(58f, 12f), 4f, new Color(0.90f, 0.96f, 1f, 0.88f));
            PaintLine(texture, new Vector2(72f, 23f), new Vector2(73f, 40f), 4f, new Color(0.90f, 0.96f, 1f, 0.88f));
            PaintCircle(texture, new Vector2(72f, 23f), 4f, new Color(0.06f, 0.17f, 0.22f, 0.92f));
            return FinishArtSprite(texture);
        }

        private static Texture2D CreateArtTexture(string name, Color top, Color bottom)
        {
            const int width = 96;
            const int height = 56;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    Color pixel = Color.Lerp(bottom, top, t);
                    if ((x * 5 + y * 3) % 19 == 0)
                        pixel = Adjust(pixel, 0.025f);

                    texture.SetPixel(x, y, pixel);
                }
            }

            return texture;
        }

        private static Sprite FinishArtSprite(Texture2D texture)
        {
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void PaintLine(Texture2D texture, Vector2 from, Vector2 to, float thickness, Color color)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) * 2f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
                PaintCircle(texture, point, thickness * 0.5f, color);
            }
        }

        private static void PaintCircle(Texture2D texture, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                        continue;

                    float edgeBlend = Mathf.Clamp01(radius - distance + 0.7f);
                    BlendPixel(texture, x, y, color, color.a * edgeBlend);
                }
            }
        }

        private static void PaintDiamond(Texture2D texture, Vector2 center, float halfWidth, float halfHeight, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - halfWidth));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + halfWidth));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - halfHeight));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + halfHeight));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float normalized = Mathf.Abs((x - center.x) / halfWidth) + Mathf.Abs((y - center.y) / halfHeight);
                    if (normalized > 1f)
                        continue;

                    BlendPixel(texture, x, y, color, color.a);
                }
            }
        }

        private static void BlendPixel(Texture2D texture, int x, int y, Color color, float amount)
        {
            Color existing = texture.GetPixel(x, y);
            Color target = new Color(color.r, color.g, color.b, 1f);
            texture.SetPixel(x, y, Color.Lerp(existing, target, Mathf.Clamp01(amount)));
        }

        private static Color Adjust(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }
    }

    private sealed class PlayerHudPanel
    {
        public Ship Ship;
        public TextMeshProUGUI Title;
        public RectTransform HpLostFill;
        public RectTransform HpFill;
        public List<RectTransform> HpDividers;
        public TextMeshProUGUI HpText;
        public ModuleSlotHud WeaponSlot;
        public ModuleSlotHud ArmorSlot;
        public ModuleSlotHud EngineSlot;
    }

    private sealed class ModuleSlotHud
    {
        public RectTransform Root;
        public Image Background;
        public Color ActiveColor;
        public IronTideModuleCardEntry Card;
        public bool ModuleEnabled;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Detail;
        public TextMeshProUGUI Passive;
        public TextMeshProUGUI Icon;
    }
}
