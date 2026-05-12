using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace IronTide.BasicCards
{
    public sealed class BasicCardShopController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform cardTemplateRoot;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private IronTideModuleCardLibrary cardLibrary;

        [Header("Shop Settings")]
        [SerializeField] private int startingGold = 10;
        [SerializeField] private int visibleShopCards = 5;
        [SerializeField] private int visibleHighPowerCards = 3;
        [SerializeField] private int basicRerollCost = 2;
        [SerializeField] private int advancedRerollCost = 5;
        [SerializeField] [Range(0.05f, 0.5f)] private float legendaryOfferChance = 0.22f;

        [Header("Window Layout")]
        [SerializeField] private Vector2 windowSize = new Vector2(1720f, 1180f);
        [SerializeField] private Vector3 windowPosition = Vector3.zero;
        [SerializeField] private Vector3 windowScale = new Vector3(0.01f, 0.01f, 0.01f);

        [Header("Visual Assets")]
        [SerializeField] private Sprite windowBodySprite;
        [SerializeField] private Sprite windowFrameSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite buttonHoverSprite;
        [SerializeField] private Sprite buttonPressedSprite;
        [SerializeField] private TMP_FontAsset headingFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        [Header("Card Layout")]
        [SerializeField] private float highPowerRowY = 220f;
        [SerializeField] private float shopRowY = -150f;
        [SerializeField] private float ownedRowY = -430f;
        [SerializeField] private float shopSlotSpacing = 270f;
        [SerializeField] private float equippedSlotSpacing = 360f;
        [SerializeField] private Vector2 slotSize = new Vector2(222f, 334f);
        [SerializeField] private Vector2 cardSize = new Vector2(198f, 292f);
        [SerializeField] private float cardOffsetY = 18f;

        [Header("Owned Module Layout")]
        [SerializeField] private Vector2 ownedSlotSize = new Vector2(178f, 120f);
        [SerializeField] private Vector2 ownedCardSize = new Vector2(88f, 88f);
        [SerializeField] private float ownedCardOffsetY = -4f;
        [SerializeField] private Vector2 ownedPreviewCardSize = new Vector2(214f, 304f);
        [SerializeField] private float ownedPreviewOffsetY = 214f;
        [SerializeField] private Vector2 shopPreviewCardSize = new Vector2(214f, 312f);
        [SerializeField] private float shopPreviewScale = 1.65f;

        private readonly List<IronTideModuleCardEntry> _availableBasicCards = new List<IronTideModuleCardEntry>();
        private readonly List<IronTideModuleCardEntry> _availableTier2Cards = new List<IronTideModuleCardEntry>();
        private readonly List<IronTideModuleCardEntry> _availableEpicCards = new List<IronTideModuleCardEntry>();
        private readonly List<IronTideModuleCardEntry> _availableLegendaryCards = new List<IronTideModuleCardEntry>();
        private readonly List<IronTideModuleCardEntry> _currentBasicCards = new List<IronTideModuleCardEntry>();
        private readonly List<IronTideModuleCardEntry> _currentAdvancedCards = new List<IronTideModuleCardEntry>();
        private readonly Dictionary<BasicModuleType, IronTideModuleCardEntry> _equippedCards =
            new Dictionary<BasicModuleType, IronTideModuleCardEntry>();
        private readonly List<CardSlotView> _basicSlots = new List<CardSlotView>();
        private readonly List<CardSlotView> _advancedSlots = new List<CardSlotView>();
        private readonly Dictionary<BasicModuleType, CardSlotView> _ownedSlots =
            new Dictionary<BasicModuleType, CardSlotView>();

        private Canvas _shopCanvas;
        private RectTransform _shopRoot;
        private RectTransform _bodyRoot;
        private Button _basicRerollButton;
        private Button _advancedRerollButton;
        private Button _nextPlayerButton;
        private Button _startGameButton;
        private TMP_Text _goldText;
        private TMP_Text _statusText;
        private TMP_Text _activePlayerText;
        private RectTransform _ownedPreviewRoot;
        private RectTransform _ownedPreviewCardRoot;
        private IronTideModuleCardView _ownedPreviewCard;
        private IronTideModuleCardEntry _previewedOwnedCard;
        private RectTransform _shopPreviewRoot;
        private RectTransform _shopPreviewCardRoot;
        private IronTideModuleCardView _shopPreviewCard;
        private IronTideModuleCardEntry _previewedShopCard;
        private int _fallbackGold;
        private int _activePlayerIndex;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            ResolveTemplateRoot();
            CacheCardPools();
            if (!enabled)
                return;

            ResolveVisualAssets();
            EnsureEventSystem();
            BuildShopWindow();
            InitializeGold();
            StartShoppingPhase();
        }

        public void StartShoppingPhase()
        {
            int playerCount = IronTideGameState.Players.Count > 0 ? IronTideGameState.Players.Count : 2;
            IronTideGameState.EnsurePlayers(playerCount);
            EnsureDirectShopGold();
            ReserveSavedLoadouts();
            _activePlayerIndex = 0;
            HideOwnedCardPreview();
            RollFreshShops();
            ShowActivePlayer();
        }

        public List<IronTideModuleCardEntry> GetEpicRewardChoices(int count)
        {
            var candidates = new List<IronTideModuleCardEntry>(_availableEpicCards);
            Shuffle(candidates);

            if (count < 1)
                count = 1;

            if (count > candidates.Count)
                count = candidates.Count;

            var results = new List<IronTideModuleCardEntry>();
            for (var i = 0; i < count; i++)
                results.Add(candidates[i]);

            return results;
        }

        public bool TryEquipRewardCard(IronTideModuleCardEntry rewardCard)
        {
            if (rewardCard == null)
                return false;

            if (_equippedCards.ContainsKey(rewardCard.SlotType))
            {
                SetStatus($"Your {GetModuleLabel(rewardCard.SlotType).ToLowerInvariant()} slot is already filled.");
                return false;
            }

            RemoveFromAvailablePool(rewardCard);
            RemoveFromCurrentRows(rewardCard);
            _equippedCards[rewardCard.SlotType] = rewardCard;
            RefreshOwnedCards();
            SaveCurrentPlayerLoadout();
            SetStatus($"Equipped reward card {rewardCard.DisplayName}.");
            return true;
        }

        internal void OnCardClicked(IronTideModuleCardEntry sourceCard, CardInteractionMode mode)
        {
            if (mode == CardInteractionMode.Shop)
                TryBuyCard(sourceCard);
            else
                TrySellCard(sourceCard);
        }

        private void ResolveTemplateRoot()
        {
            if (cardTemplateRoot == null)
            {
                var templateObject = GameObject.Find("Basic Module Cards");
                if (templateObject != null)
                    cardTemplateRoot = templateObject.transform;
            }

            if (cardTemplateRoot != null)
                cardTemplateRoot.gameObject.SetActive(false);
        }

        private void CacheCardPools()
        {
            ResolveCardLibrary();

            if (cardLibrary == null)
            {
                Debug.LogError("BasicCardShopController requires an IronTideModuleCardLibrary asset.");
                enabled = false;
                return;
            }

            _availableBasicCards.Clear();
            _availableTier2Cards.Clear();
            _availableEpicCards.Clear();
            _availableLegendaryCards.Clear();

            foreach (var card in cardLibrary.Cards)
            {
                if (card == null || !card.IsValid)
                    continue;

                if (card.AppearsInBasicShop)
                {
                    _availableBasicCards.Add(card);
                    continue;
                }

                if (card.IsTier2)
                {
                    _availableTier2Cards.Add(card);
                    continue;
                }

                if (card.IsEpic)
                {
                    _availableEpicCards.Add(card);
                    continue;
                }

                if (card.IsLegendary)
                    _availableLegendaryCards.Add(card);
            }
        }

        private void ResolveCardLibrary()
        {
            if (cardLibrary != null)
                return;

            cardLibrary = Resources.Load<IronTideModuleCardLibrary>("IronTideModuleLibrary");

#if UNITY_EDITOR
            if (cardLibrary == null)
            {
                cardLibrary = UnityEditor.AssetDatabase.LoadAssetAtPath<IronTideModuleCardLibrary>(
                    "Assets/IronTide/BasicCards/Data/IronTideModuleLibrary.asset");
            }
#endif
        }

        private void ResolveVisualAssets()
        {
#if UNITY_EDITOR
            if (windowBodySprite == null)
            {
                windowBodySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/SlimUI/Modern Menu 1/Graphics/Frames/Panel 1920x1080px.png");
            }

            if (windowFrameSprite == null)
            {
                windowFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/SlimUI/Modern Menu 1/Graphics/Frames/Panel Frame 512px.png");
            }

            if (buttonSprite == null)
            {
                buttonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Fram 256px.png");
            }

            if (buttonHoverSprite == null)
            {
                buttonHoverSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Frame Hover 256px.png");
            }

            if (buttonPressedSprite == null)
            {
                buttonPressedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/SlimUI/Modern Menu 1/Graphics/Buttons/Button Frame Press 256px.png");
            }

            if (headingFont == null)
            {
                headingFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/SlimUI/Modern Menu 1/Fonts/LATO-BOLD SDF.asset");
            }

            if (bodyFont == null)
            {
                bodyFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/SlimUI/Modern Menu 1/Fonts/LATO-LIGHT SDF.asset");
            }
#endif
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

        private void BuildShopWindow()
        {
            windowSize = new Vector2(1920f, 1080f);
            windowPosition = Vector3.zero;
            windowScale = Vector3.one;

            Vector2 fullScreenSize = windowSize;
            Vector2 bodySize = fullScreenSize - new Vector2(32f, 32f);

            highPowerRowY = 210f;
            shopRowY = -132f;
            ownedRowY = -418f;
            shopSlotSpacing = 286f;
            slotSize = new Vector2(238f, 348f);
            cardSize = new Vector2(214f, 312f);
            cardOffsetY = 14f;
            ownedSlotSize = new Vector2(190f, 126f);
            ownedCardSize = new Vector2(98f, 94f);
            equippedSlotSpacing = 240f;
            float equippedCenterX = -300f;

            var canvasObject = new GameObject("Basic Shop Window");
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = windowPosition;
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = windowScale;

            _shopCanvas = canvasObject.AddComponent<Canvas>();
            _shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _shopCanvas.overrideSorting = true;
            _shopCanvas.sortingOrder = 200;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = fullScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            _shopRoot = canvasObject.GetComponent<RectTransform>();
            _shopRoot.anchorMin = Vector2.zero;
            _shopRoot.anchorMax = Vector2.one;
            _shopRoot.offsetMin = Vector2.zero;
            _shopRoot.offsetMax = Vector2.zero;
            _shopRoot.sizeDelta = Vector2.zero;

            var border = CreatePanel("Border", _shopRoot, fullScreenSize, new Color(0.03f, 0.05f, 0.08f, 1f));
            StretchToParent(border, 0f);
            var body = CreatePanel("Body", border, bodySize, new Color(0.07f, 0.11f, 0.16f, 1f));
            StretchToParent(body, 16f);
            _bodyRoot = body;

            CreateLabel("Title", body, "Shopping Phase", 36, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(700f, 50f),
                TextAlignmentOptions.Center, new Color(0.96f, 0.89f, 0.73f, 1f), headingFont);

            _activePlayerText = CreateLabel("ActivePlayer", body, string.Empty, 24, FontStyles.Bold,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -54f), new Vector2(320f, 44f),
                TextAlignmentOptions.Left, new Color(0.96f, 0.89f, 0.73f, 1f), headingFont);

            _basicRerollButton = CreateButton(body, "RerollBasicButton", $"Reroll T1 ({basicRerollCost}g)",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-310f, -58f), new Vector2(250f, 58f),
                RerollBasicShop, new Color(0.48f, 0.62f, 0.27f, 0.96f), headingFont,
                buttonSprite, buttonHoverSprite, buttonPressedSprite);

            _advancedRerollButton = CreateButton(body, "RerollAdvancedButton", $"Reroll High ({advancedRerollCost}g)",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-310f, -126f), new Vector2(250f, 58f),
                RerollAdvancedShop, new Color(0.76f, 0.61f, 0.18f, 0.96f), headingFont,
                buttonSprite, buttonHoverSprite, buttonPressedSprite);

            CreateLabel("AdvancedHeading", body, "Tier 2, Epic & Legendary Modules", 24, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, highPowerRowY + slotSize.y * 0.5f + 38f), new Vector2(420f, 30f),
                TextAlignmentOptions.Center, new Color(0.96f, 0.89f, 0.73f, 1f), headingFont);

            CreateLabel("BasicHeading", body, "Basic Modules", 24, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, shopRowY + slotSize.y * 0.5f + 38f), new Vector2(360f, 30f),
                TextAlignmentOptions.Center, new Color(0.96f, 0.89f, 0.73f, 1f), headingFont);

            CreateLabel("OwnedHeading", body, "Equipped Modules", 24, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(equippedCenterX, ownedRowY + ownedSlotSize.y * 0.5f + 42f), new Vector2(360f, 30f),
                TextAlignmentOptions.Center, new Color(0.96f, 0.89f, 0.73f, 1f), headingFont);

            _statusText = CreateLabel("Status", body, string.Empty, 20, FontStyles.Normal,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 46f), new Vector2(820f, 58f),
                TextAlignmentOptions.Left, new Color(0.96f, 0.84f, 0.57f, 1f), bodyFont);

            _goldText = CreateLabel("Gold", body, string.Empty, 24, FontStyles.Bold,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 46f), new Vector2(220f, 46f),
                TextAlignmentOptions.Right, new Color(0.99f, 0.87f, 0.46f, 1f), headingFont);

            _nextPlayerButton = CreateButton(body, "NextPlayerButton", "Next Player",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-590f, 48f), new Vector2(220f, 60f),
                NextPlayer, new Color(0.25f, 0.43f, 0.62f, 0.96f), headingFont,
                buttonSprite, buttonHoverSprite, buttonPressedSprite);

            _startGameButton = CreateButton(body, "StartGameButton", "Start Game 2",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-340f, 48f), new Vector2(250f, 60f),
                FinishShoppingAndLoadGame2, new Color(0.62f, 0.29f, 0.20f, 0.96f), headingFont,
                buttonSprite, buttonHoverSprite, buttonPressedSprite);

            BuildShopRow(body, _advancedSlots, visibleHighPowerCards, highPowerRowY);
            BuildShopRow(body, _basicSlots, visibleShopCards, shopRowY);

            _ownedSlots[BasicModuleType.Weapon] = CreateCardSlot(body, "WeaponSlot",
                new Vector2(equippedCenterX - equippedSlotSpacing, ownedRowY), true, BasicModuleType.Weapon,
                ownedSlotSize, ownedCardSize, ownedCardOffsetY, headingFont, bodyFont, buttonSprite, buttonSprite, true);
            _ownedSlots[BasicModuleType.Armor] = CreateCardSlot(body, "ArmorSlot",
                new Vector2(equippedCenterX, ownedRowY), true, BasicModuleType.Armor,
                ownedSlotSize, ownedCardSize, ownedCardOffsetY, headingFont, bodyFont, buttonSprite, buttonSprite, true);
            _ownedSlots[BasicModuleType.Engine] = CreateCardSlot(body, "EngineSlot",
                new Vector2(equippedCenterX + equippedSlotSpacing, ownedRowY), true, BasicModuleType.Engine,
                ownedSlotSize, ownedCardSize, ownedCardOffsetY, headingFont, bodyFont, buttonSprite, buttonSprite, true);

            _ownedPreviewRoot = CreatePanel("OwnedPreview", body, ownedPreviewCardSize + new Vector2(18f, 18f),
                new Color(0.08f, 0.11f, 0.16f, 0.94f), buttonSprite, Image.Type.Sliced);
            _ownedPreviewRoot.gameObject.SetActive(false);
            _ownedPreviewRoot.SetAsLastSibling();

            _ownedPreviewCardRoot = new GameObject("PreviewCardRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _ownedPreviewCardRoot.SetParent(_ownedPreviewRoot, false);
            _ownedPreviewCardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _ownedPreviewCardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _ownedPreviewCardRoot.pivot = new Vector2(0.5f, 0.5f);
            _ownedPreviewCardRoot.anchoredPosition = Vector2.zero;
            _ownedPreviewCardRoot.sizeDelta = ownedPreviewCardSize;

            var shopPreviewSize = (shopPreviewCardSize * shopPreviewScale) + new Vector2(28f, 28f);
            _shopPreviewRoot = CreatePanel("ShopCardPreview", body, shopPreviewSize,
                new Color(0.02f, 0.03f, 0.05f, 0.98f), buttonSprite, Image.Type.Sliced);
            _shopPreviewRoot.gameObject.SetActive(false);
            _shopPreviewRoot.SetAsLastSibling();

            _shopPreviewCardRoot = new GameObject("ShopPreviewCardRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _shopPreviewCardRoot.SetParent(_shopPreviewRoot, false);
            _shopPreviewCardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _shopPreviewCardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _shopPreviewCardRoot.pivot = new Vector2(0.5f, 0.5f);
            _shopPreviewCardRoot.anchoredPosition = Vector2.zero;
            _shopPreviewCardRoot.sizeDelta = shopPreviewCardSize;
            _shopPreviewCardRoot.localScale = Vector3.one * shopPreviewScale;
        }

        private void BuildShopRow(RectTransform body, List<CardSlotView> slots, int count, float rowY)
        {
            slots.Clear();

            var targetCount = Mathf.Max(1, count);
            var centerOffset = (targetCount - 1) * 0.5f;
            for (var i = 0; i < targetCount; i++)
            {
                var slotX = (i - centerOffset) * shopSlotSpacing;
                slots.Add(CreateCardSlot(body, $"ShopSlot_{rowY}_{i + 1}", new Vector2(slotX, rowY), false,
                    BasicModuleType.Armor, slotSize, cardSize, cardOffsetY, headingFont, bodyFont, null, buttonSprite, false));
            }
        }

        private void InitializeGold()
        {
            _fallbackGold = startingGold;
            UpdateGoldText();
        }

        private int CurrentGold
        {
            get
            {
                var player = ActivePlayer;
                if (player != null)
                    return player.Gold;

                return _fallbackGold;
            }
            set
            {
                _fallbackGold = value;

                var player = ActivePlayer;
                if (player != null)
                    player.Gold = value;

                if (UIManager.Instance != null && UIManager.Instance.InformationCanvas != null)
                    UIManager.Instance.InformationCanvas.SetGoldText(value);

                UpdateGoldText();
            }
        }

        private IronTidePlayerState ActivePlayer => IronTideGameState.GetPlayer(_activePlayerIndex);

        private void EnsureDirectShopGold()
        {
            if (IronTideGameState.HasSavedLoadouts)
                return;

            foreach (IronTidePlayerState player in IronTideGameState.Players)
            {
                if (player != null && player.Gold <= 0)
                    player.Gold = startingGold;
            }
        }

        private void ReserveSavedLoadouts()
        {
            foreach (IronTidePlayerState player in IronTideGameState.Players)
            {
                if (player == null)
                    continue;

                RemoveFromAvailablePool(cardLibrary.FindById(player.WeaponModuleId));
                RemoveFromAvailablePool(cardLibrary.FindById(player.ArmorModuleId));
                RemoveFromAvailablePool(cardLibrary.FindById(player.EngineModuleId));
            }
        }

        private void ShowActivePlayer()
        {
            LoadPlayerLoadout(ActivePlayer);
            RefreshOwnedCards();
            UpdateGoldText();

            int displayIndex = _activePlayerIndex + 1;
            int playerCount = Mathf.Max(1, IronTideGameState.Players.Count);

            if (_activePlayerText != null)
                _activePlayerText.text = $"Player {displayIndex}/{playerCount}";

            if (_activePlayerIndex >= playerCount - 1)
                SetStatus($"Player {displayIndex} shopping. Press Start Game 2 when done.");
            else
                SetStatus($"Player {displayIndex} shopping. Buy, sell, or press Next Player.");

            UpdateRerollButton();
        }

        private void LoadPlayerLoadout(IronTidePlayerState player)
        {
            _equippedCards.Clear();

            if (player == null || cardLibrary == null)
                return;

            AddEquippedCard(cardLibrary.FindById(player.WeaponModuleId));
            AddEquippedCard(cardLibrary.FindById(player.ArmorModuleId));
            AddEquippedCard(cardLibrary.FindById(player.EngineModuleId));
        }

        private void AddEquippedCard(IronTideModuleCardEntry card)
        {
            if (card == null)
                return;

            _equippedCards[card.SlotType] = card;
        }

        private void SaveCurrentPlayerLoadout()
        {
            _equippedCards.TryGetValue(BasicModuleType.Weapon, out var weapon);
            _equippedCards.TryGetValue(BasicModuleType.Armor, out var armor);
            _equippedCards.TryGetValue(BasicModuleType.Engine, out var engine);
            IronTideGameState.UpdatePlayerLoadout(_activePlayerIndex, weapon, armor, engine);
        }

        private void NextPlayer()
        {
            SaveCurrentPlayerLoadout();

            int playerCount = IronTideGameState.Players.Count;
            if (_activePlayerIndex >= playerCount - 1)
            {
                SetStatus("All players are ready. Press Start Game 2.");
                UpdateRerollButton();
                return;
            }

            _activePlayerIndex++;
            HideOwnedCardPreview();
            ShowActivePlayer();
        }

        private void FinishShoppingAndLoadGame2()
        {
            SaveCurrentPlayerLoadout();
            IronTideGameState.CompleteShopping();
            SceneManager.LoadScene(IronTideGameState.CombatSceneName);
        }

        private void RerollBasicShop()
        {
            if (CurrentGold < basicRerollCost)
            {
                SetStatus("Not enough gold to reroll Tier 1 modules.");
                return;
            }

            CurrentGold -= basicRerollCost;
            RollFreshShop(_availableBasicCards, _currentBasicCards, _basicSlots.Count);
            RefreshShopCards();
            SetStatus($"Rerolled Tier 1 modules for {basicRerollCost} gold.");
        }

        private void RerollAdvancedShop()
        {
            if (CurrentGold < advancedRerollCost)
            {
                SetStatus("Not enough gold to reroll high-power modules.");
                return;
            }

            CurrentGold -= advancedRerollCost;
            RollFreshAdvancedShop();
            RefreshShopCards();
            SetStatus($"Rerolled high-power modules for {advancedRerollCost} gold.");
        }

        private void RollFreshShops()
        {
            RollFreshShop(_availableBasicCards, _currentBasicCards, _basicSlots.Count);
            RollFreshAdvancedShop();
            RefreshShopCards();
        }

        private void RollFreshShop(List<IronTideModuleCardEntry> sourceCards, List<IronTideModuleCardEntry> currentCards, int visibleCount)
        {
            currentCards.Clear();

            var candidates = new List<IronTideModuleCardEntry>(sourceCards);
            Shuffle(candidates);

            var count = Mathf.Min(visibleCount, candidates.Count);
            for (var i = 0; i < count; i++)
                currentCards.Add(candidates[i]);
        }

        private void RollFreshAdvancedShop()
        {
            _currentAdvancedCards.Clear();

            var advancedPool = new List<IronTideModuleCardEntry>(_availableTier2Cards);
            advancedPool.AddRange(_availableEpicCards);
            var legendaryPool = new List<IronTideModuleCardEntry>(_availableLegendaryCards);

            for (var i = 0; i < _advancedSlots.Count; i++)
            {
                var tryLegendary = legendaryPool.Count > 0 && Random.value < legendaryOfferChance;
                IronTideModuleCardEntry chosenCard = null;

                if (tryLegendary)
                    chosenCard = DrawRandomCard(legendaryPool);

                if (chosenCard == null)
                    chosenCard = DrawRandomCard(advancedPool);

                if (chosenCard == null)
                    chosenCard = DrawRandomCard(legendaryPool);

                if (chosenCard == null)
                    break;

                _currentAdvancedCards.Add(chosenCard);
            }
        }

        private void RefreshShopCards()
        {
            HideShopCardPreview();
            RefreshSlotList(_advancedSlots, _currentAdvancedCards, CardInteractionMode.Shop);
            RefreshSlotList(_basicSlots, _currentBasicCards, CardInteractionMode.Shop);
            UpdateRerollButton();
        }

        private void RefreshOwnedCards()
        {
            RefreshOwnedSlot(BasicModuleType.Weapon);
            RefreshOwnedSlot(BasicModuleType.Armor);
            RefreshOwnedSlot(BasicModuleType.Engine);
        }

        private void RefreshOwnedSlot(BasicModuleType moduleType)
        {
            var slot = _ownedSlots[moduleType];
            if (_equippedCards.TryGetValue(moduleType, out var card))
                PopulateSlot(slot, card, CardInteractionMode.Equipped);
            else
                ClearSlot(slot, $"Empty {GetModuleLabel(moduleType)} Slot");
        }

        private void RefreshSlotList(List<CardSlotView> slots, List<IronTideModuleCardEntry> cards, CardInteractionMode mode)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (i < cards.Count)
                    PopulateSlot(slots[i], cards[i], mode);
                else
                    ClearSlot(slots[i], "No card");
            }
        }

        private void PopulateSlot(CardSlotView slot, IronTideModuleCardEntry sourceCard, CardInteractionMode mode)
        {
            DestroySlotCard(slot);

            var viewObject = new GameObject($"{sourceCard.DisplayName}_{mode}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(IronTideModuleCardView));
            viewObject.transform.SetParent(slot.CardRoot, false);

            var rectTransform = viewObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            var cardView = viewObject.GetComponent<IronTideModuleCardView>();
            var visualMode = mode == CardInteractionMode.Equipped ? CardVisualMode.Compact : CardVisualMode.Standard;
            cardView.Initialize(this, sourceCard, mode, visualMode);

            var button = viewObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = viewObject.GetComponent<Image>();
            button.onClick.AddListener(() => OnCardClicked(sourceCard, mode));

            slot.ActiveCard = cardView;
            slot.Placeholder.text = string.Empty;
            if (slot.PriceFrame != null)
                slot.PriceFrame.enabled = mode == CardInteractionMode.Shop;
            slot.PriceLabel.text = mode == CardInteractionMode.Shop
                ? $"{sourceCard.BuyCost}g"
                : $"Sell {sourceCard.SellValue}g";
        }

        private void ClearSlot(CardSlotView slot, string placeholderText)
        {
            DestroySlotCard(slot);
            slot.Placeholder.text = placeholderText;
            slot.PriceLabel.text = string.Empty;
            if (slot.PriceFrame != null)
                slot.PriceFrame.enabled = false;
        }

        private void DestroySlotCard(CardSlotView slot)
        {
            if (slot.ActiveCard == null)
                return;

            Destroy(slot.ActiveCard.gameObject);
            slot.ActiveCard = null;
        }

        private void TryBuyCard(IronTideModuleCardEntry sourceCard)
        {
            if (sourceCard == null)
                return;

            if (_equippedCards.TryGetValue(sourceCard.SlotType, out var equippedCard) && equippedCard != null)
            {
                SetStatus($"{GetModuleLabel(sourceCard.SlotType)} slot is occupied. Sell {equippedCard.DisplayName} first.");
                return;
            }

            if (CurrentGold < sourceCard.BuyCost)
            {
                SetStatus($"You need {sourceCard.BuyCost} gold to buy {sourceCard.DisplayName}.");
                return;
            }

            CurrentGold -= sourceCard.BuyCost;

            _equippedCards[sourceCard.SlotType] = sourceCard;

            RemoveFromAvailablePool(sourceCard);
            RemoveFromCurrentRows(sourceCard);
            FillOpenShopSlots();

            RefreshOwnedCards();
            RefreshShopCards();
            SaveCurrentPlayerLoadout();

            SetStatus($"Bought {sourceCard.DisplayName}.");
        }

        private void TrySellCard(IronTideModuleCardEntry sourceCard)
        {
            if (sourceCard == null)
                return;

            if (!_equippedCards.TryGetValue(sourceCard.SlotType, out var equippedCard))
                return;

            if (equippedCard != sourceCard)
                return;

            HideOwnedCardPreview(sourceCard);
            _equippedCards.Remove(sourceCard.SlotType);
            ReturnToOriginPool(sourceCard);
            CurrentGold += sourceCard.SellValue;

            RefreshOwnedCards();
            UpdateRerollButton();
            SaveCurrentPlayerLoadout();
            SetStatus($"Sold {sourceCard.DisplayName} for {sourceCard.SellValue} gold.");
        }

        private void RemoveFromAvailablePool(IronTideModuleCardEntry card)
        {
            if (card == null)
                return;

            if (card.AppearsInBasicShop)
            {
                _availableBasicCards.Remove(card);
                return;
            }

            if (card.IsTier2)
            {
                _availableTier2Cards.Remove(card);
                return;
            }

            if (card.IsEpic)
            {
                _availableEpicCards.Remove(card);
                return;
            }

            if (card.IsLegendary)
                _availableLegendaryCards.Remove(card);
        }

        private void ReturnToOriginPool(IronTideModuleCardEntry card)
        {
            if (card == null)
                return;

            if (card.AppearsInBasicShop)
            {
                if (!_availableBasicCards.Contains(card))
                    _availableBasicCards.Add(card);
                return;
            }

            if (card.IsTier2)
            {
                if (!_availableTier2Cards.Contains(card))
                    _availableTier2Cards.Add(card);
                return;
            }

            if (card.IsEpic)
            {
                if (!_availableEpicCards.Contains(card))
                    _availableEpicCards.Add(card);
                return;
            }

            if (card.IsLegendary && !_availableLegendaryCards.Contains(card))
                _availableLegendaryCards.Add(card);
        }

        private void RemoveFromCurrentRows(IronTideModuleCardEntry card)
        {
            _currentBasicCards.Remove(card);
            _currentAdvancedCards.Remove(card);
        }

        private void FillOpenShopSlots()
        {
            FillOpenShopSlots(_availableBasicCards, _currentBasicCards, _basicSlots.Count);
            FillOpenAdvancedShopSlots();
        }

        private void FillOpenShopSlots(List<IronTideModuleCardEntry> sourceCards, List<IronTideModuleCardEntry> currentCards, int visibleCount)
        {
            if (currentCards.Count >= visibleCount)
                return;

            var candidates = new List<IronTideModuleCardEntry>();
            foreach (var card in sourceCards)
            {
                if (!currentCards.Contains(card))
                    candidates.Add(card);
            }

            Shuffle(candidates);

            foreach (var candidate in candidates)
            {
                if (currentCards.Count >= visibleCount)
                    break;

                currentCards.Add(candidate);
            }
        }

        private void FillOpenAdvancedShopSlots()
        {
            if (_currentAdvancedCards.Count >= _advancedSlots.Count)
                return;

            var advancedPool = new List<IronTideModuleCardEntry>(_availableTier2Cards);
            advancedPool.AddRange(_availableEpicCards);
            var legendaryPool = new List<IronTideModuleCardEntry>(_availableLegendaryCards);

            foreach (var card in _currentAdvancedCards)
            {
                advancedPool.Remove(card);
                legendaryPool.Remove(card);
            }

            while (_currentAdvancedCards.Count < _advancedSlots.Count)
            {
                var tryLegendary = legendaryPool.Count > 0 && Random.value < legendaryOfferChance;
                IronTideModuleCardEntry chosenCard = null;

                if (tryLegendary)
                    chosenCard = DrawRandomCard(legendaryPool);

                if (chosenCard == null)
                    chosenCard = DrawRandomCard(advancedPool);

                if (chosenCard == null)
                    chosenCard = DrawRandomCard(legendaryPool);

                if (chosenCard == null)
                    break;

                _currentAdvancedCards.Add(chosenCard);
            }
        }

        private void UpdateGoldText()
        {
            if (_goldText != null)
                _goldText.text = $"Gold: {CurrentGold}";

            UpdateRerollButton();
        }

        private void UpdateRerollButton()
        {
            if (_basicRerollButton != null)
                _basicRerollButton.interactable = CurrentGold >= basicRerollCost && _availableBasicCards.Count > 0;

            if (_advancedRerollButton != null)
            {
                _advancedRerollButton.interactable = CurrentGold >= advancedRerollCost &&
                    (_availableTier2Cards.Count > 0 || _availableEpicCards.Count > 0 || _availableLegendaryCards.Count > 0);
            }

            int playerCount = IronTideGameState.Players.Count;

            if (_nextPlayerButton != null)
                _nextPlayerButton.interactable = playerCount > 1 && _activePlayerIndex < playerCount - 1;

            if (_startGameButton != null)
                _startGameButton.interactable = playerCount > 0 && _activePlayerIndex >= playerCount - 1;
        }

        private static IronTideModuleCardEntry DrawRandomCard(List<IronTideModuleCardEntry> cards)
        {
            if (cards == null || cards.Count == 0)
                return null;

            var randomIndex = Random.Range(0, cards.Count);
            var card = cards[randomIndex];
            cards.RemoveAt(randomIndex);
            return card;
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        internal void ShowOwnedCardPreview(IronTideModuleCardEntry sourceCard)
        {
            if (sourceCard == null || _ownedPreviewRoot == null || _ownedPreviewCardRoot == null)
                return;

            if (!_ownedSlots.TryGetValue(sourceCard.SlotType, out var slot))
                return;

            _ownedPreviewRoot.anchoredPosition = GetOwnedPreviewPosition(slot, sourceCard.SlotType);
            _ownedPreviewRoot.gameObject.SetActive(true);
            _ownedPreviewRoot.SetAsLastSibling();

            if (_previewedOwnedCard == sourceCard && _ownedPreviewCard != null)
                return;

            ClearOwnedPreviewCard();

            var previewObject = new GameObject($"{sourceCard.DisplayName}_Preview",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(IronTideModuleCardView));
            previewObject.transform.SetParent(_ownedPreviewCardRoot, false);

            var rectTransform = previewObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            _ownedPreviewCard = previewObject.GetComponent<IronTideModuleCardView>();
            _ownedPreviewCard.Initialize(this, sourceCard, CardInteractionMode.Preview, CardVisualMode.Standard);
            _previewedOwnedCard = sourceCard;
        }

        internal void HideOwnedCardPreview(IronTideModuleCardEntry sourceCard = null)
        {
            if (sourceCard != null && _previewedOwnedCard != sourceCard)
                return;

            if (_ownedPreviewRoot != null)
                _ownedPreviewRoot.gameObject.SetActive(false);

            ClearOwnedPreviewCard();
            _previewedOwnedCard = null;
        }

        internal void ShowShopCardPreview(IronTideModuleCardEntry sourceCard)
        {
            if (sourceCard == null || _shopPreviewRoot == null || _shopPreviewCardRoot == null)
                return;

            _shopPreviewRoot.anchoredPosition = GetShopPreviewPosition(sourceCard);
            _shopPreviewRoot.gameObject.SetActive(true);
            _shopPreviewRoot.SetAsLastSibling();

            if (_previewedShopCard == sourceCard && _shopPreviewCard != null)
                return;

            ClearShopPreviewCard();

            var previewObject = new GameObject($"{sourceCard.DisplayName}_ShopPreview",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(IronTideModuleCardView));
            previewObject.transform.SetParent(_shopPreviewCardRoot, false);

            var rectTransform = previewObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            var previewImage = previewObject.GetComponent<Image>();
            previewImage.raycastTarget = false;

            _shopPreviewCard = previewObject.GetComponent<IronTideModuleCardView>();
            _shopPreviewCard.Initialize(this, sourceCard, CardInteractionMode.Preview, CardVisualMode.Standard);
            previewObject.GetComponent<Image>().raycastTarget = false;
            _previewedShopCard = sourceCard;
        }

        internal void HideShopCardPreview(IronTideModuleCardEntry sourceCard = null)
        {
            if (sourceCard != null && _previewedShopCard != sourceCard)
                return;

            if (_shopPreviewRoot != null)
                _shopPreviewRoot.gameObject.SetActive(false);

            ClearShopPreviewCard();
            _previewedShopCard = null;
        }

        private Vector2 GetShopPreviewPosition(IronTideModuleCardEntry sourceCard)
        {
            var slot = FindShopSlot(sourceCard);
            var previewSize = (shopPreviewCardSize * shopPreviewScale) + new Vector2(28f, 28f);
            var bodySize = windowSize - new Vector2(34f, 34f);
            var halfBody = bodySize * 0.5f;
            var halfPreview = previewSize * 0.5f;
            var inset = 28f;

            var previewPosition = new Vector2(650f, -34f);
            if (slot != null)
            {
                float side = slot.Root.anchoredPosition.x >= 0f ? -1f : 1f;
                previewPosition.x = slot.Root.anchoredPosition.x + side * 360f;
                previewPosition.y = slot.Root.anchoredPosition.y + 18f;
            }

            previewPosition.x = Mathf.Clamp(previewPosition.x,
                -halfBody.x + halfPreview.x + inset,
                halfBody.x - halfPreview.x - inset);
            previewPosition.y = Mathf.Clamp(previewPosition.y,
                -halfBody.y + halfPreview.y + inset,
                halfBody.y - halfPreview.y - inset);

            return previewPosition;
        }

        private CardSlotView FindShopSlot(IronTideModuleCardEntry sourceCard)
        {
            var slot = FindShopSlot(_advancedSlots, sourceCard);
            if (slot != null)
                return slot;

            return FindShopSlot(_basicSlots, sourceCard);
        }

        private static CardSlotView FindShopSlot(List<CardSlotView> slots, IronTideModuleCardEntry sourceCard)
        {
            foreach (var slot in slots)
            {
                if (slot.ActiveCard != null && slot.ActiveCard.Card == sourceCard)
                    return slot;
            }

            return null;
        }

        private Vector2 GetOwnedPreviewPosition(CardSlotView slot, BasicModuleType slotType)
        {
            var previewSize = ownedPreviewCardSize + new Vector2(18f, 18f);
            var sideOffset = (ownedSlotSize.x * 0.5f) + (previewSize.x * 0.5f) + 28f;
            var defaultY = slot.Root.anchoredPosition.y + ownedPreviewOffsetY;
            var previewPosition = new Vector2(slot.Root.anchoredPosition.x, defaultY);

            switch (slotType)
            {
                case BasicModuleType.Weapon:
                    previewPosition.x -= sideOffset;
                    previewPosition.y -= 18f;
                    break;
                case BasicModuleType.Engine:
                    previewPosition.x += sideOffset;
                    previewPosition.y -= 18f;
                    break;
                case BasicModuleType.Armor:
                    previewPosition.y += 18f;
                    break;
            }

            var bodySize = windowSize - new Vector2(34f, 34f);
            var halfBody = bodySize * 0.5f;
            var halfPreview = previewSize * 0.5f;
            var inset = 28f;

            previewPosition.x = Mathf.Clamp(previewPosition.x,
                -halfBody.x + halfPreview.x + inset,
                halfBody.x - halfPreview.x - inset);
            previewPosition.y = Mathf.Clamp(previewPosition.y,
                -halfBody.y + halfPreview.y + inset,
                halfBody.y - halfPreview.y - inset);

            return previewPosition;
        }

        private void ClearOwnedPreviewCard()
        {
            if (_ownedPreviewCard == null)
                return;

            Destroy(_ownedPreviewCard.gameObject);
            _ownedPreviewCard = null;
        }

        private void ClearShopPreviewCard()
        {
            if (_shopPreviewCard == null)
                return;

            Destroy(_shopPreviewCard.gameObject);
            _shopPreviewCard = null;
        }

        private static string GetModuleLabel(BasicModuleType moduleType)
        {
            switch (moduleType)
            {
                case BasicModuleType.Armor:
                    return "Armor";
                case BasicModuleType.Weapon:
                    return "Weapon";
                case BasicModuleType.Engine:
                    return "Engine";
                default:
                    return "Module";
            }
        }

        private static void Shuffle(List<IronTideModuleCardEntry> cards)
        {
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var randomIndex = Random.Range(0, i + 1);
                var temp = cards[i];
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 size, Color color,
            Sprite sprite = null, Image.Type imageType = Image.Type.Simple)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = panelObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? imageType : Image.Type.Simple;
            image.raycastTarget = false;
            return rect;
        }

        private static void StretchToParent(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static TMP_Text CreateLabel(string name, RectTransform parent, string text, float fontSize,
            FontStyles fontStyle, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            TextAlignmentOptions alignment, Color color, TMP_FontAsset font = null)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            if (font != null)
                label.font = font;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, fontSize * 0.62f);
            label.fontSizeMax = fontSize;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            return label;
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action, Color color,
            TMP_FontAsset font, Sprite normalSprite, Sprite hoverSprite, Sprite pressedSprite)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            image.sprite = normalSprite;
            image.type = normalSprite != null ? Image.Type.Sliced : Image.Type.Simple;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            if (hoverSprite != null || pressedSprite != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = hoverSprite,
                    selectedSprite = hoverSprite,
                    pressedSprite = pressedSprite
                };
            }

            CreateLabel("Label", rect, text, 22, FontStyles.Bold, Vector2.zero, Vector2.one, Vector2.zero,
                Vector2.zero, TextAlignmentOptions.Center, Color.white, font);

            return button;
        }

        private static CardSlotView CreateCardSlot(RectTransform parent, string name, Vector2 position, bool ownedSlot,
            BasicModuleType moduleType, Vector2 slotSize, Vector2 cardSize, float cardOffsetY,
            TMP_FontAsset headingFont, TMP_FontAsset bodyFont, Sprite slotSprite, Sprite priceSprite, bool showBackground)
        {
            var slotObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slotObject.transform.SetParent(parent, false);

            var slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = position;
            slotRect.sizeDelta = slotSize;

            var background = slotObject.GetComponent<Image>();
            background.color = ownedSlot
                ? new Color(0.19f, 0.25f, 0.32f, 0.92f)
                : new Color(1f, 1f, 1f, 0f);
            background.sprite = slotSprite;
            background.type = slotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.enabled = showBackground || slotSprite != null;
            background.raycastTarget = false;

            var cardRoot = new GameObject("CardRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            cardRoot.SetParent(slotRect, false);
            cardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cardRoot.pivot = new Vector2(0.5f, 0.5f);
            cardRoot.anchoredPosition = new Vector2(0f, cardOffsetY);
            cardRoot.sizeDelta = cardSize;

            var placeholder = CreateLabel("Placeholder", slotRect,
                ownedSlot ? $"Empty {GetModuleLabel(moduleType)} Slot" : "No card",
                ownedSlot ? 16 : 18, FontStyles.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f),
                new Vector2(150f, 88f), TextAlignmentOptions.Center, new Color(0.92f, 0.94f, 0.98f, 0.8f), bodyFont);

            Image priceFrame = null;
            TMP_Text priceLabel;

            if (ownedSlot)
            {
                priceLabel = CreateLabel("Price", slotRect, string.Empty, 14, FontStyles.Bold,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(150f, 26f),
                    TextAlignmentOptions.Center, new Color(1f, 0.87f, 0.54f, 1f), headingFont);
            }
            else
            {
                var priceFrameObject = new GameObject("PriceFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                priceFrameObject.transform.SetParent(slotRect, false);

                var priceFrameRect = priceFrameObject.GetComponent<RectTransform>();
                priceFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
                priceFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
                priceFrameRect.pivot = new Vector2(0.5f, 0.5f);
                priceFrameRect.anchoredPosition = new Vector2(0f, cardOffsetY - (cardSize.y * 0.5f) + 28f);
                priceFrameRect.sizeDelta = new Vector2(Mathf.Clamp(cardSize.x - 54f, 96f, 144f), 34f);

                priceFrame = priceFrameObject.GetComponent<Image>();
                priceFrame.color = new Color(0.45f, 0.29f, 0.14f, 0.98f);
                priceFrame.sprite = priceSprite;
                priceFrame.type = priceSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                priceFrame.enabled = false;
                priceFrame.raycastTarget = false;

                priceLabel = CreateLabel("Price", priceFrameRect, string.Empty, 18, FontStyles.Bold,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.7f, 1f), headingFont);
                priceLabel.textWrappingMode = TextWrappingModes.NoWrap;
            }

            if (ownedSlot)
            {
                CreateLabel("ModuleLabel", slotRect, $"{GetModuleLabel(moduleType)} Slot", 16, FontStyles.Bold,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(140f, 22f),
                    TextAlignmentOptions.Center, new Color(0.96f, 0.89f, 0.73f, 1f), headingFont);
            }

            return new CardSlotView(slotRect, cardRoot, placeholder, priceLabel, priceFrame);
        }

        private sealed class CardSlotView
        {
            public CardSlotView(RectTransform root, RectTransform cardRoot, TMP_Text placeholder, TMP_Text priceLabel, Image priceFrame)
            {
                Root = root;
                CardRoot = cardRoot;
                Placeholder = placeholder;
                PriceLabel = priceLabel;
                PriceFrame = priceFrame;
            }

            public RectTransform Root { get; }
            public RectTransform CardRoot { get; }
            public TMP_Text Placeholder { get; }
            public TMP_Text PriceLabel { get; }
            public Image PriceFrame { get; }
            public IronTideModuleCardView ActiveCard { get; set; }
        }
    }

    public enum CardInteractionMode
    {
        Shop = 0,
        Equipped = 1,
        Preview = 2
    }
}
