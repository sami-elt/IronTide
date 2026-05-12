using System.Collections.Generic;
using IronTide.BasicCards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private TextMeshProUGUI sidebarArmor;
    private TextMeshProUGUI sidebarLastAction;

    private bool gameOver;
    private int winnerNumber;
    private string lastActionText = "Waiting for first move.";

    private static readonly Color PanelColor = new Color(0.05f, 0.08f, 0.13f, 0.92f);
    private static readonly Color HeaderColor = new Color(0.10f, 0.16f, 0.25f, 0.96f);
    private static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.32f, 1f);
    private static readonly Color TextColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.70f, 0.78f, 1f);
    private static readonly Color ActiveModuleColor = new Color(0.12f, 0.22f, 0.34f, 0.98f);
    private static readonly Color BrokenModuleColor = new Color(0.31f, 0.07f, 0.07f, 0.98f);
    private static readonly Color EmptyModuleColor = new Color(0.10f, 0.10f, 0.11f, 0.86f);
    private static readonly Color HpHealthyColor = new Color(0.22f, 0.78f, 0.32f, 1f);
    private static readonly Color HpDangerColor = new Color(0.95f, 0.24f, 0.20f, 1f);

    private void Awake()
    {
        ResolveModuleLibrary();

        if (autoDealStarterModules)
            DealStarterModules();
    }

    private void Start()
    {
        HideLegacyLabels();
        BuildHud();

        TurnManager.OnTurnStarted += HandleTurnStarted;
        TurnManager.OnMovementRolled += HandleMovementRolled;
        TurnManager.OnAttackRolled += HandleAttackRolled;
        TurnManager.OnDamageDealt += HandleDamageDealt;

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

    private void DealStarterModules()
    {
        if (moduleLibrary == null)
        {
            Debug.LogWarning("TestDay1PlayUI could not find IronTideModuleLibrary. Module HUD will show empty slots.");
            return;
        }

        var weapons = Shuffled(GetTier1(BasicModuleType.Weapon));
        var armors = Shuffled(GetTier1(BasicModuleType.Armor));
        var engines = Shuffled(GetTier1(BasicModuleType.Engine));

        for (int i = 0; i < ships.Count; i++)
        {
            Ship ship = ships[i];
            if (ship == null || ship.shipInfo == null)
                continue;

            ShipInfo info = ship.shipInfo;
            if (info.WeaponModule == null && i < weapons.Count)
                info.SetWeaponModule(weapons[i]);
            if (info.ArmorModule == null && i < armors.Count)
                info.SetArmorModule(armors[i]);
            if (info.EngineModule == null && i < engines.Count)
                info.SetEngineModule(engines[i]);

            info.ResetValues();
        }
    }

    private List<IronTideModuleCardEntry> GetTier1(BasicModuleType slotType)
    {
        var results = new List<IronTideModuleCardEntry>();
        if (moduleLibrary == null)
            return results;

        foreach (IronTideModuleCardEntry card in moduleLibrary.Cards)
        {
            if (card != null && card.IsValid && card.Tier == IronTideCardTier.Tier1 && card.SlotType == slotType)
                results.Add(card);
        }

        return results;
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

        BuildSidebar(transform);
        BuildModuleStrip(transform);
    }

    private void BuildSidebar(Transform parent)
    {
        RectTransform sidebar = CreatePanel("Playtest Sidebar", parent, PanelColor);
        sidebar.anchorMin = new Vector2(0f, 0.16f);
        sidebar.anchorMax = new Vector2(0f, 1f);
        sidebar.pivot = new Vector2(0f, 0.5f);
        sidebar.offsetMin = new Vector2(0f, 0f);
        sidebar.offsetMax = new Vector2(340f, 0f);

        var layout = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 10;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        sidebarTitle = CreateLabel(sidebar, "Current Player", 20f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        sidebarTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        sidebarPhase = CreateInfoLine(sidebar, "Phase: -");
        sidebarMovement = CreateInfoLine(sidebar, "Move: -");
        sidebarWeapon = CreateInfoLine(sidebar, "Weapon: -");
        sidebarArmor = CreateInfoLine(sidebar, "Armor: -");
        sidebarLastAction = CreateInfoLine(sidebar, lastActionText);
    }

    private TextMeshProUGUI CreateInfoLine(RectTransform parent, string text)
    {
        TextMeshProUGUI label = CreateLabel(parent, text, 16f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        return label;
    }

    private void BuildModuleStrip(Transform parent)
    {
        RectTransform strip = CreatePanel("Ship Module Strip", parent, PanelColor);
        strip.anchorMin = new Vector2(0f, 0f);
        strip.anchorMax = new Vector2(1f, 0f);
        strip.pivot = new Vector2(0.5f, 0f);
        strip.offsetMin = new Vector2(0f, 0f);
        strip.offsetMax = new Vector2(0f, 156f);

        var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 12;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        playerPanels.Clear();
        foreach (Ship ship in ships)
        {
            if (ship == null)
                continue;

            playerPanels.Add(BuildPlayerPanel(strip, ship));
        }
    }

    private PlayerHudPanel BuildPlayerPanel(RectTransform parent, Ship ship)
    {
        RectTransform panel = CreatePanel("Player Module Panel", parent, HeaderColor);
        panel.gameObject.AddComponent<LayoutElement>().preferredWidth = 560f;

        var verticalLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 8, 8);
        verticalLayout.spacing = 8;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        int playerNumber = ship.turnPlayerController != null ? ship.turnPlayerController.playerID + 1 : playerPanels.Count + 1;
        TextMeshProUGUI title = CreateLabel(panel, $"Player {playerNumber}", 17f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform hpRoot = CreatePanel("HP Bar", panel, new Color(0.15f, 0.04f, 0.04f, 1f));
        hpRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform hpFill = CreatePanel("HP Fill", hpRoot, HpHealthyColor);
        hpFill.anchorMin = Vector2.zero;
        hpFill.anchorMax = Vector2.one;
        hpFill.offsetMin = Vector2.zero;
        hpFill.offsetMax = Vector2.zero;

        TextMeshProUGUI hpText = CreateLabel(hpRoot, "10/10", 12f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(hpText.rectTransform);

        RectTransform cards = CreatePanel("Module Cards", panel, new Color(0f, 0f, 0f, 0f));
        cards.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

        var cardLayout = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardLayout.spacing = 8;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = true;

        return new PlayerHudPanel
        {
            Ship = ship,
            HpFill = hpFill,
            HpText = hpText,
            WeaponSlot = BuildModuleSlot(cards, "WEAPON"),
            ArmorSlot = BuildModuleSlot(cards, "ARMOR"),
            EngineSlot = BuildModuleSlot(cards, "ENGINE")
        };
    }

    private ModuleSlotHud BuildModuleSlot(RectTransform parent, string label)
    {
        RectTransform root = CreatePanel(label, parent, EmptyModuleColor);
        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 1;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI header = CreateLabel(root, label, 11f, FontStyles.Bold, GoldColor, TextAlignmentOptions.Left);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;

        TextMeshProUGUI name = CreateLabel(root, "-", 13f, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        name.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        TextMeshProUGUI detail = CreateLabel(root, "-", 11f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Left);
        detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        return new ModuleSlotHud
        {
            Background = root.GetComponent<Image>(),
            Name = name,
            Detail = detail
        };
    }

    private void RefreshSidebar()
    {
        Ship currentShip = GetCurrentShip();
        if (currentShip == null || currentShip.shipInfo == null)
            return;

        int playerNumber = currentShip.turnPlayerController != null ? currentShip.turnPlayerController.playerID + 1 : 1;
        sidebarTitle.SetText($"Player {playerNumber}'s Turn");
        sidebarPhase.SetText($"Phase: {GetPhaseLabel()}");
        sidebarMovement.SetText($"Move: {currentShip.shipMovement.avaliableTileDistance} tiles left");
        sidebarWeapon.SetText($"Weapon: {GetCardName(currentShip.shipInfo.WeaponModule)} | Range {currentShip.shipInfo.GetWeaponRange()}");
        sidebarArmor.SetText($"Armor: {currentShip.shipInfo.GetArmor()}");
        sidebarLastAction.SetText(lastActionText);
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
        float hpRatio = info.MaxHealth > 0 ? Mathf.Clamp01((float)info.Health / info.MaxHealth) : 0f;
        panel.HpFill.anchorMax = new Vector2(hpRatio, 1f);
        panel.HpFill.GetComponent<Image>().color = info.Health <= 3 ? HpDangerColor : HpHealthyColor;
        panel.HpText.SetText($"{info.Health}/{info.MaxHealth}");

        RefreshModuleSlot(panel.WeaponSlot, info.WeaponModule, info.WeaponEnabled);
        RefreshModuleSlot(panel.ArmorSlot, info.ArmorModule, info.ArmorEnabled);
        RefreshModuleSlot(panel.EngineSlot, info.EngineModule, info.EngineEnabled);
    }

    private void RefreshModuleSlot(ModuleSlotHud slot, IronTideModuleCardEntry card, bool enabled)
    {
        if (slot == null)
            return;

        if (card == null || !card.IsValid)
        {
            slot.Background.color = EmptyModuleColor;
            slot.Name.SetText("Empty");
            slot.Detail.SetText("No card equipped");
            return;
        }

        slot.Background.color = enabled ? ActiveModuleColor : BrokenModuleColor;
        slot.Name.SetText(card.DisplayName);

        string dice = card.UsesDice ? $"{card.DiceCount}xD{card.DiceSides}" : "No dice";
        string passive = card.HasPassive ? card.PassiveName : "No passive";
        slot.Detail.SetText($"{card.ModifierLabel} | {dice}\n{(enabled ? passive : "Damaged")}");
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
                winIndicator.SetText($"Player {winnerNumber} won!");
                winIndicator.gameObject.SetActive(true);
            }
        }
    }

    private void HandleTurnStarted(int playerId)
    {
        lastActionText = "New turn started.";
    }

    private void HandleMovementRolled(int total)
    {
        lastActionText = $"Movement rolled: {total} tiles.";
    }

    private void HandleAttackRolled(int total)
    {
        lastActionText = $"Attack rolled: {total} raw damage.";
    }

    private void HandleDamageDealt(int damage)
    {
        lastActionText = $"Damage dealt after armor: {damage}.";
    }

    private static string GetCardName(IronTideModuleCardEntry card)
    {
        return card != null && card.IsValid ? card.DisplayName : "None";
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
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9f;
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

    private sealed class PlayerHudPanel
    {
        public Ship Ship;
        public RectTransform HpFill;
        public TextMeshProUGUI HpText;
        public ModuleSlotHud WeaponSlot;
        public ModuleSlotHud ArmorSlot;
        public ModuleSlotHud EngineSlot;
    }

    private sealed class ModuleSlotHud
    {
        public Image Background;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Detail;
    }
}
