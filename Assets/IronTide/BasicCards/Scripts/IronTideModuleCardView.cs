using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IronTide.BasicCards
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class IronTideModuleCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BasicCardShopController _controller;
        private IronTideModuleCardEntry _card;
        private CardInteractionMode _mode;
        private CardVisualMode _visualMode;

        private Image _background;
        private Image _frame;
        private Image _topBand;
        private TMP_Text _iconText;
        private TMP_Text _tierText;
        private TMP_Text _titleText;
        private TMP_Text _diceText;
        private TMP_Text _modifierText;
        private Image _artFrame;
        private Image _artImage;
        private TMP_Text _artPlaceholderText;
        private Image _rulesPanel;
        private TMP_Text _rulesText;

        private Vector3 _initialScale;
        private Color _baseColor;

        internal IronTideModuleCardEntry Card => _card;

        public void Initialize(BasicCardShopController controller, IronTideModuleCardEntry card,
            CardInteractionMode mode, CardVisualMode visualMode = CardVisualMode.Standard)
        {
            _controller = controller;
            _card = card;
            _mode = mode;
            _visualMode = visualMode;

            EnsureBuilt();
            ApplyCard();
            _initialScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = _initialScale * (_visualMode == CardVisualMode.Compact ? 1.08f : 1.02f);
            _background.color = Tint(_baseColor, 0.05f);

            if (_mode == CardInteractionMode.Equipped && _visualMode == CardVisualMode.Compact)
                _controller?.ShowOwnedCardPreview(_card);
            else if (_mode == CardInteractionMode.Shop)
                _controller?.ShowShopCardPreview(_card);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _initialScale;
            _background.color = _baseColor;

            if (_mode == CardInteractionMode.Equipped && _visualMode == CardVisualMode.Compact)
                _controller?.HideOwnedCardPreview(_card);
            else if (_mode == CardInteractionMode.Shop)
                _controller?.HideShopCardPreview(_card);
        }

        private void EnsureBuilt()
        {
            if (_frame != null)
                return;

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _background = GetComponent<Image>();
            _background.raycastTarget = true;

            if (_visualMode == CardVisualMode.Compact)
                BuildCompactLayout(rectTransform);
            else
                BuildStandardLayout(rectTransform);
        }

        private void BuildStandardLayout(RectTransform rectTransform)
        {
            _frame = CreateImage("Frame", rectTransform, new Color(0f, 0f, 0f, 0.14f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(204f, 300f));

            _topBand = CreateImage("TopBand", rectTransform, new Color(0f, 0f, 0f, 0.15f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(204f, 56f));

            var iconBadge = CreateImage("IconBadge", _topBand.rectTransform, new Color(1f, 1f, 1f, 0.17f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(36f, 36f));
            _iconText = CreateText("IconText", iconBadge.rectTransform, 16, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, Color.white, 11f, 16f);

            _tierText = CreateText("TierText", _topBand.rectTransform, 14, FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(78f, 22f),
                TextAlignmentOptions.Left, new Color(0.95f, 0.97f, 1f, 0.98f), 10f, 14f);

            var diceBadge = CreateImage("DiceBadge", _topBand.rectTransform, new Color(1f, 1f, 1f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(42f, 36f));
            _diceText = CreateText("DiceText", diceBadge.rectTransform, 13, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, new Color(0.25f, 0.27f, 0.31f, 1f), 9f, 13f);

            var modifierBubble = CreateImage("ModifierBubble", _topBand.rectTransform, new Color(1f, 1f, 1f, 0.2f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(42f, 42f));
            _modifierText = CreateText("ModifierText", modifierBubble.rectTransform, 22, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, Color.white, 14f, 22f);

            _titleText = CreateText("TitleText", rectTransform, 23, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(184f, 34f),
                TextAlignmentOptions.Center, Color.white, 13f, 23f);

            _artFrame = CreateImage("ArtFrame", rectTransform, new Color(1f, 1f, 1f, 0.92f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(184f, 44f));

            _artImage = CreateImage("ArtImage", _artFrame.rectTransform, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _artImage.preserveAspect = true;

            _artPlaceholderText = CreateText("ArtPlaceholder", _artFrame.rectTransform, 17, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, new Color(0.34f, 0.37f, 0.42f, 1f), 11f, 17f);

            _rulesPanel = CreateImage("RulesPanel", rectTransform, new Color(0f, 0f, 0f, 0.22f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(190f, 154f));

            _rulesText = CreateText("RulesText", _rulesPanel.rectTransform, 14.5f, FontStyles.Normal,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-18f, -16f),
                TextAlignmentOptions.TopLeft, new Color(0.98f, 0.99f, 1f, 1f), 10.5f, 14.5f);
            _rulesText.margin = new Vector4(10f, 8f, 10f, 8f);
            _rulesText.lineSpacing = -1f;
        }

        private void BuildCompactLayout(RectTransform rectTransform)
        {
            _frame = CreateImage("Frame", rectTransform, new Color(0f, 0f, 0f, 0.18f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 88f));

            _topBand = CreateImage("TopBand", rectTransform, new Color(0f, 0f, 0f, 0.2f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(92f, 20f));

            _tierText = CreateText("TierText", _topBand.rectTransform, 10, FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(44f, 16f),
                TextAlignmentOptions.Left, new Color(0.96f, 0.97f, 1f, 0.98f), 7.5f, 10f);

            var modifierBubble = CreateImage("ModifierBubble", rectTransform, new Color(1f, 1f, 1f, 0.2f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(32f, 32f));
            _modifierText = CreateText("ModifierText", modifierBubble.rectTransform, 16, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, Color.white, 10f, 16f);

            _artFrame = CreateImage("ArtFrame", rectTransform, new Color(1f, 1f, 1f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(58f, 40f));

            _artImage = CreateImage("ArtImage", _artFrame.rectTransform, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _artImage.preserveAspect = true;

            _artPlaceholderText = CreateText("ArtPlaceholder", _artFrame.rectTransform, 11, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, new Color(0.34f, 0.37f, 0.42f, 1f), 8f, 11f);

            _iconText = CreateText("IconText", rectTransform, 14, FontStyles.Bold,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(78f, 16f),
                TextAlignmentOptions.Center, Color.white, 9f, 14f);
        }

        private void ApplyCard()
        {
            _baseColor = GetTierColor(_card.Tier);
            _background.color = _visualMode == CardVisualMode.Compact
                ? new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.92f)
                : _baseColor;

            _frame.color = Tint(_baseColor, -0.08f);
            if (_topBand != null)
                _topBand.color = Tint(_baseColor, -0.14f);

            if (_visualMode == CardVisualMode.Compact)
                ApplyCompactCard();
            else
                ApplyStandardCard();
        }

        private void ApplyStandardCard()
        {
            if (_rulesPanel != null)
                _rulesPanel.color = Tint(_baseColor, -0.3f);

            if (_iconText != null)
                _iconText.text = _card.IconLabel;
            if (_tierText != null)
                _tierText.text = _card.TierLabel;
            if (_titleText != null)
                _titleText.text = _card.DisplayName;
            if (_diceText != null)
                _diceText.text = _card.DiceLabel;
            if (_modifierText != null)
                _modifierText.text = _card.ModifierLabel;

            ApplyArtwork();

            if (_rulesText != null)
                _rulesText.text = BuildBodyText(_card);
        }

        private void ApplyCompactCard()
        {
            if (_tierText != null)
                _tierText.text = _card.IconLabel;
            if (_modifierText != null)
                _modifierText.text = _card.ModifierLabel;
            if (_iconText != null)
                _iconText.text = _card.DisplayName;

            ApplyArtwork();
        }

        private void ApplyArtwork()
        {
            if (_artImage == null || _artPlaceholderText == null)
                return;

            if (_card.ArtworkSprite != null)
            {
                _artImage.sprite = _card.ArtworkSprite;
                _artImage.color = Color.white;
                _artPlaceholderText.gameObject.SetActive(false);
            }
            else
            {
                _artImage.sprite = null;
                _artImage.color = new Color(1f, 1f, 1f, 0f);
                _artPlaceholderText.text = _visualMode == CardVisualMode.Compact ? _card.IconLabel : _card.ArchetypeLabel;
                _artPlaceholderText.gameObject.SetActive(true);
            }
        }

        private static string BuildBodyText(IronTideModuleCardEntry card)
        {
            if (card == null)
                return string.Empty;

            var baseBlock = $"<b>{card.BaseRulesTitle}</b>\n{card.BaseRulesText}";
            if (!card.HasPassive)
                return baseBlock;

            return $"{baseBlock}\n\n<b>{card.PassiveName}:</b>\n{card.PassiveDescription}";
        }

        private static Image CreateImage(string name, RectTransform parent, Color color, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, float fontSize, FontStyles fontStyle,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta,
            TextAlignmentOptions alignment, Color color, float minSize, float maxSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = minSize;
            label.fontSizeMax = maxSize;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Color GetTierColor(IronTideCardTier tier)
        {
            switch (tier)
            {
                case IronTideCardTier.Tier1:
                    return new Color(0.44f, 0.67f, 0.36f, 1f);
                case IronTideCardTier.Tier2:
                    return new Color(0.58f, 0.54f, 0.8f, 1f);
                case IronTideCardTier.Legendary:
                    return new Color(0.77f, 0.62f, 0.24f, 1f);
                case IronTideCardTier.Epic:
                    return new Color(0.69f, 0.39f, 0.48f, 1f);
                default:
                    return new Color(0.42f, 0.46f, 0.53f, 1f);
            }
        }

        private static Color Tint(Color color, float delta)
        {
            return new Color(
                Mathf.Clamp01(color.r + delta),
                Mathf.Clamp01(color.g + delta),
                Mathf.Clamp01(color.b + delta),
                color.a);
        }
    }

    public enum CardVisualMode
    {
        Standard = 0,
        Compact = 1
    }
}
