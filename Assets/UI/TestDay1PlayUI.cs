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
    public List<Ship> ships;

    [Header("Module Test Setup")]
    [SerializeField] private IronTideModuleCardLibrary moduleLibrary;
    [SerializeField] private bool autoDealStarterModules = true;

    private readonly List<PlayerHudPanel> playerPanels = new List<PlayerHudPanel>();
    private TextMeshProUGUI sidebarTitle;
    private TextMeshProUGUI sidebarPhase;
    private TextMeshProUGUI sidebarMovement;
    private TextMeshProUGUI sidebarWeapon;
    private TextMeshProUGUI sidebarAttack;
    private TextMeshProUGUI sidebarArmor;
    private TextMeshProUGUI sidebarLastAction;
    private RectTransform moduleTooltip;
    private TextMeshProUGUI tooltipTitle;
    private TextMeshProUGUI tooltipMeta;
    private TextMeshProUGUI tooltipStats;
    private TextMeshProUGUI tooltipPassive;
    private ModuleSlotHud hoveredSlot;

    private bool gameOver;
    private int winnerNumber;
    private bool transitionStarted;
    private string lastActionText = "Waiting for first move.";
    private string attackText = "Attack not rolled.";
    private string previousAttackText = string.Empty;

    private static readonly Color PanelColor = new Color(0.05f, 0.08f, 0.13f, 0.92f);
    private static readonly Color HeaderColor = new Color(0.10f, 0.16f, 0.25f, 0.96f);
    private static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.32f, 1f);
    private static readonly Color TextColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.70f, 0.78f, 1f);
    private static readonly Color WeaponModuleColor = new Color(0.23f, 0.12f, 0.10f, 0.98f);
    private static readonly Color ArmorModuleColor = new Color(0.15f, 0.18f, 0.22f, 0.98f);
    private static readonly Color EngineModuleColor = new Color(0.08f, 0.19f, 0.19f, 0.98f);
    private static readonly Color BrokenModuleColor = new Color(0.31f, 0.07f, 0.07f, 0.98f);
    private static readonly Color EmptyModuleColor = new Color(0.07f, 0.09f, 0.12f, 0.86f);
    private static readonly Color TooltipColor = new Color(0.04f, 0.06f, 0.09f, 0.98f);
    private static readonly Color HpHealthyColor = new Color(0.22f, 0.78f, 0.32f, 1f);
    private static readonly Color HpDangerColor = new Color(0.95f, 0.24f, 0.20f, 1f);
    private static readonly Color HpDividerColor = new Color(0.98f, 0.96f, 0.86f, 0.95f);
    private const string GoldHex = "#F2C84B";
    private const string MoveHex = "#89D8FF";
    private const string TextHex = "#EAF2FF";
    private const string MutedHex = "#A6B6C9";

    private void Awake()
    {
        ResolveModuleLibrary();
        ResolveShipReferences();
        SetupPlayerModules();
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
        RefreshSidebar();
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
            if (IronTideGameState.ShouldOpenShopAfterCombat && autoDealStarterModules)
                DealStarterWeapons();
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
        BuildSidebar(transform);
        BuildModuleTooltip(transform);
    }

    private void BuildSidebar(Transform parent)
    {
        RectTransform sidebar = CreatePanel("Playtest Sidebar", parent, PanelColor);
        sidebar.anchorMin = new Vector2(0f, 0f);
        sidebar.anchorMax = new Vector2(0f, 1f);
        sidebar.pivot = new Vector2(0f, 0.5f);
        sidebar.offsetMin = new Vector2(0f, 0f);
        sidebar.offsetMax = new Vector2(390f, 0f);

        var layout = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 12);
        layout.spacing = 7;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        sidebarTitle = CreateLabel(sidebar, "Current Player", 22f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        sidebarTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        sidebarPhase = CreateInfoLine(sidebar, "Phase: -");
        sidebarMovement = CreateInfoLine(sidebar, "Move: -");
        sidebarWeapon = CreateInfoLine(sidebar, "Weapon: -");
        sidebarAttack = CreateInfoLine(sidebar, "Attack: -");
        sidebarArmor = CreateInfoLine(sidebar, "Armor: -");
        sidebarLastAction = CreateInfoLine(sidebar, lastActionText);

        TextMeshProUGUI modulesHeader = CreateLabel(sidebar, "Ship Modules", 17f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        modulesHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        playerPanels.Clear();
        foreach (Ship ship in ships)
        {
            if (ship == null)
                continue;

            playerPanels.Add(BuildPlayerPanel(sidebar, ship));
        }
    }

    private TextMeshProUGUI CreateInfoLine(RectTransform parent, string text)
    {
        TextMeshProUGUI label = CreateLabel(parent, text, 16f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        label.richText = true;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 31f;
        return label;
    }

    private RectTransform CreateModuleRow(RectTransform parent)
    {
        RectTransform row = CreatePanel("Module Row", parent, new Color(0f, 0f, 0f, 0f));
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 58f;
        rowLayout.flexibleHeight = 0f;

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childAlignment = TextAnchor.UpperLeft;
        return row;
    }

    private PlayerHudPanel BuildPlayerPanel(RectTransform parent, Ship ship)
    {
        RectTransform panel = CreatePanel("Player Module Panel", parent, HeaderColor);
        LayoutElement panelLayout = panel.gameObject.AddComponent<LayoutElement>();
        panelLayout.preferredHeight = 116f;
        panelLayout.flexibleHeight = 0f;

        var verticalLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(9, 9, 7, 7);
        verticalLayout.spacing = 4;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childControlHeight = true;

        RectTransform header = CreatePanel("Player Header", panel, new Color(0f, 0f, 0f, 0f));
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 6;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;

        int playerId = GetShipPlayerId(ship, playerPanels.Count);
        Image icon = CreatePlayerIcon(header, playerId);
        TextMeshProUGUI title = CreateLabel(header, IronTideGameState.GetPlayerDisplayName(playerId), 16f,
            FontStyles.Bold, IronTideGameState.GetPlayerColor(playerId, GoldColor), TextAlignmentOptions.Left);
        var titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 20f;
        titleLayout.flexibleWidth = 1f;

        RectTransform hpRoot = CreatePanel("HP Bar", panel, new Color(0.15f, 0.04f, 0.04f, 1f));
        hpRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform hpFill = CreatePanel("HP Fill", hpRoot, HpHealthyColor);
        hpFill.anchorMin = Vector2.zero;
        hpFill.anchorMax = Vector2.one;
        hpFill.offsetMin = Vector2.zero;
        hpFill.offsetMax = Vector2.zero;

        List<RectTransform> hpDividers = BuildHpDividers(hpRoot, 2);

        TextMeshProUGUI hpText = CreateLabel(hpRoot, "10/10", 12f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(hpText.rectTransform);

        RectTransform cards = CreateModuleRow(panel);

        return new PlayerHudPanel
        {
            Ship = ship,
            Title = title,
            Icon = icon,
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
        slotLayout.minHeight = 54f;
        slotLayout.preferredHeight = 54f;
        slotLayout.flexibleHeight = 0f;
        root.GetComponent<Image>().raycastTarget = true;

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 0;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;

        TextMeshProUGUI header = CreateLabel(root, label, 10f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;

        TextMeshProUGUI name = CreateLabel(root, "-", 13f, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        name.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;

        TextMeshProUGUI detail = CreateLabel(root, "-", 11f, FontStyles.Bold, MutedTextColor, TextAlignmentOptions.Left);
        detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

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
        moduleTooltip.anchorMin = new Vector2(0f, 0.5f);
        moduleTooltip.anchorMax = new Vector2(0f, 0.5f);
        moduleTooltip.pivot = new Vector2(0f, 0.5f);
        moduleTooltip.sizeDelta = new Vector2(520f, 238f);
        moduleTooltip.anchoredPosition = new Vector2(400f, -160f);

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

    private void RefreshSidebar()
    {
        Ship currentShip = GetCurrentShip();
        if (currentShip == null || currentShip.shipInfo == null)
            return;

        int playerId = GetShipPlayerId(currentShip, 0);
        sidebarTitle.SetText($"{IronTideGameState.GetPlayerDisplayName(playerId)}'s Turn");
        sidebarTitle.color = IronTideGameState.GetPlayerColor(playerId, GoldColor);
        sidebarPhase.SetText(FormatInfoLine("PHASE", GetPhaseLabel(), GoldHex));
        sidebarMovement.SetText(FormatInfoLine("MOVE", $"{currentShip.shipMovement.avaliableTileDistance} tiles left", MoveHex));
        sidebarWeapon.SetText(FormatInfoLine("WEAPON", $"{GetCardName(currentShip.shipInfo.WeaponModule)} | Range {currentShip.shipInfo.GetWeaponRange()}", TextHex));
        sidebarAttack.SetText(FormatInfoLine("ATTACK", GetAttackLabel(currentShip), GoldHex));
        sidebarArmor.SetText(FormatInfoLine("ARMOR", currentShip.shipInfo.GetArmor().ToString(), MutedHex));
        sidebarLastAction.SetText(FormatInfoLine("LAST", lastActionText, GoldHex));
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

        switch (TurnManager.Instance.currentPhase)
        {
            case TurnPhase.RollMovement:
                return "Roll Movement [M]";
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
        RefreshPlayerIcon(panel.Icon, playerId);

        float hpRatio = info.MaxHealth > 0 ? Mathf.Clamp01((float)info.Health / info.MaxHealth) : 0f;
        panel.HpFill.anchorMax = new Vector2(hpRatio, 1f);
        panel.HpFill.GetComponent<Image>().color = info.Health <= 3 || info.GetActiveModuleAmount() <= 1
            ? HpDangerColor
            : HpHealthyColor;
        panel.HpText.SetText($"{info.Health}/{info.MaxHealth}");
        RefreshHpDividers(panel, info.MaxHealth);

        RefreshModuleSlot(panel.WeaponSlot, info.WeaponModule, info.WeaponEnabled);
        RefreshModuleSlot(panel.ArmorSlot, info.ArmorModule, info.ArmorEnabled);
        RefreshModuleSlot(panel.EngineSlot, info.EngineModule, info.EngineEnabled);
    }

    private void RefreshHpDividers(PlayerHudPanel panel, int maxHealth)
    {
        if (panel.HpDividers == null)
            return;

        int dividerCount = Mathf.Max(0, (maxHealth / ShipInfo.HealthPerModule) - 1);
        for (int i = 0; i < panel.HpDividers.Count; i++)
        {
            RectTransform divider = panel.HpDividers[i];
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
            slot.Detail.SetText("No card");

            if (hoveredSlot == slot)
                HideModuleTooltip(slot);

            return;
        }

        slot.Background.color = enabled ? slot.ActiveColor : BrokenModuleColor;
        slot.Name.SetText(card.DisplayName);
        slot.Detail.SetText(enabled ? GetModuleBonusLabel(card) : "Damaged");

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
                return Mathf.Abs(card.BaseModifier) == 1 ? $"{modifier} tile" : $"{modifier} tiles";
            default:
                return $"{modifier} damage";
        }
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
                winIndicator.SetText($"{IronTideGameState.GetPlayerDisplayName(winnerNumber - 1)} won!");
                winIndicator.gameObject.SetActive(true);
            }

            FinishRound(winnerNumber - 1);
        }
    }

    private void FinishRound(int winnerPlayerId)
    {
        if (transitionStarted)
            return;

        IronTideGameState.SaveLoadouts(ships);

        if (!IronTideGameState.ShouldOpenShopAfterCombat)
            return;

        transitionStarted = true;
        IronTideGameState.AwardShopGold(winnerPlayerId);
        SceneManager.LoadScene(IronTideGameState.ShoppingSceneName);
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
            return $"Damage {damage}\nDice {dice} | Bonus {card.ModifierLabel} | {economy}";
        }

        if (card.SlotType == BasicModuleType.Armor)
            return $"Armor {card.ModifierLabel}\nEach active module adds +10 HP | {economy}";

        string tileWord = Mathf.Abs(card.BaseModifier) == 1 ? "tile" : "tiles";
        return $"Movement {card.ModifierLabel} {tileWord}\nMove roll {dice} + engine bonus | {economy}";
    }

    private static string GetCardDamageRangeLabel(IronTideModuleCardEntry card)
    {
        if (card == null || !card.IsValid || !card.UsesDice)
            return "1-6";

        int minDamage = card.DiceCount + card.BaseModifier;
        int maxDamage = card.DiceCount * card.DiceSides + card.BaseModifier;

        if (card.PassiveKey == "perfect_shot_t1" ||
            card.PassiveKey == "perfect_shot_t2" ||
            card.PassiveKey == "lucky")
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
            return $"Choose target | {targetText}";
        }

        return "Press A when a target is in range.";
    }

    private static string FormatInfoLine(string label, string value, string valueColor)
    {
        return $"<color={GoldHex}><b>{label}</b></color>  <color={valueColor}>{value}</color>";
    }

    private static string FormatSignedNumber(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
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

    private static Image CreatePlayerIcon(RectTransform parent, int playerId)
    {
        RectTransform iconRoot = CreatePanel("Player Icon", parent, new Color(1f, 1f, 1f, 0.2f));
        var layout = iconRoot.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 18f;
        layout.preferredHeight = 18f;
        layout.flexibleWidth = 0f;

        Image icon = iconRoot.GetComponent<Image>();
        icon.preserveAspect = true;
        RefreshPlayerIcon(icon, playerId);
        return icon;
    }

    private static void RefreshPlayerIcon(Image icon, int playerId)
    {
        if (icon == null)
            return;

        Sprite playerIcon = IronTideGameState.GetPlayerIcon(playerId);
        icon.sprite = playerIcon;
        icon.color = playerIcon != null
            ? Color.white
            : IronTideGameState.GetPlayerColor(playerId, GoldColor);
    }

    private sealed class PlayerHudPanel
    {
        public Ship Ship;
        public TextMeshProUGUI Title;
        public Image Icon;
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
    }
}
