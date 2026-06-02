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
        private Image _moduleIconImage;
        private TMP_Text _iconText;
        private TMP_Text _tierText;
        private Image _diceBadge;
        private TMP_Text _titleText;
        private TMP_Text _diceText;
        private Image _modifierBubble;
        private TMP_Text _modifierText;
        private Image _artFrame;
        private Image _artImage;
        private TMP_Text _artPlaceholderText;
        private Image _rulesPanel;
        private TMP_Text _rulesText;

        private Vector3 _initialScale;
        private Color _baseColor;
        private Sprite _moduleIconSprite;

        internal IronTideModuleCardEntry Card => _card;

        public void Initialize(BasicCardShopController controller, IronTideModuleCardEntry card,
            CardInteractionMode mode, CardVisualMode visualMode = CardVisualMode.Standard, Sprite moduleIconSprite = null)
        {
            _controller = controller;
            _card = card;
            _mode = mode;
            _visualMode = visualMode;
            _moduleIconSprite = moduleIconSprite;

            EnsureBuilt();
            ApplyCard();
            _initialScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = _initialScale * (_visualMode == CardVisualMode.Compact ? 1.08f : 1.02f);
            if (_visualMode == CardVisualMode.Compact)
                _background.color = Tint(_baseColor, 0.05f);
            else
                _frame.color = Tint(_baseColor, 0.02f);

            if (_mode == CardInteractionMode.Equipped && _visualMode == CardVisualMode.Compact)
                _controller?.ShowOwnedCardPreview(_card);
            else if (_mode == CardInteractionMode.Shop)
                _controller?.ShowShopCardPreview(_card);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _initialScale;
            if (_visualMode == CardVisualMode.Compact)
                _background.color = _baseColor;
            else
                _frame.color = Tint(_baseColor, -0.08f);

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
            _frame = CreateImage("Frame", rectTransform, new Color(0.015f, 0.018f, 0.020f, 0.42f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(206f, 268f));

            _topBand = CreateImage("TopBand", rectTransform, new Color(0.02f, 0.025f, 0.028f, 0.48f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(196f, 64f));

            var iconBadge = CreateImage("IconBadge", _topBand.rectTransform, new Color(0.88f, 0.94f, 0.84f, 0.88f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(44f, 44f));
            _moduleIconImage = CreateImage("ModuleIcon", iconBadge.rectTransform, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            _moduleIconImage.preserveAspect = true;
            _iconText = CreateText("IconText", iconBadge.rectTransform, 16, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, Color.white, 11f, 16f);

            _tierText = CreateText("TierText", _topBand.rectTransform, 11.5f, FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 12f), new Vector2(60f, 16f),
                TextAlignmentOptions.Left, new Color(0.88f, 0.93f, 1f, 0.84f), 8.5f, 11.5f);

            _titleText = CreateText("TitleText", _topBand.rectTransform, 14f, FontStyles.Bold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, -11f), new Vector2(92f, 28f),
                TextAlignmentOptions.Left, Color.white, 9f, 14f);
            _titleText.overflowMode = TextOverflowModes.Ellipsis;

            _diceBadge = CreateImage("DiceBadge", _topBand.rectTransform, new Color(0.92f, 0.90f, 0.82f, 0.86f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 12f), new Vector2(38f, 23f));
            _diceText = CreateText("DiceText", _diceBadge.rectTransform, 11.2f, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, new Color(0.18f, 0.19f, 0.21f, 1f), 8f, 11.2f);

            _modifierBubble = CreateImage("ModifierBubble", _topBand.rectTransform, new Color(0.86f, 0.95f, 0.82f, 0.24f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, -15f), new Vector2(38f, 25f));
            _modifierText = CreateText("ModifierText", _modifierBubble.rectTransform, 15.5f, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, Color.white, 10f, 15.5f);

            _artFrame = CreateImage("ArtFrame", rectTransform, new Color(1f, 1f, 1f, 0.92f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(184f, 44f));
            _artFrame.gameObject.SetActive(false);

            _artImage = CreateImage("ArtImage", _artFrame.rectTransform, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _artImage.preserveAspect = true;

            _artPlaceholderText = CreateText("ArtPlaceholder", _artFrame.rectTransform, 17, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, new Color(0.34f, 0.37f, 0.42f, 1f), 11f, 17f);

            _rulesPanel = CreateImage("RulesPanel", rectTransform, new Color(0.015f, 0.025f, 0.018f, 0.52f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(186f, 156f));

            _rulesText = CreateText("RulesText", _rulesPanel.rectTransform, 12.2f, FontStyles.Normal,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-18f, -16f),
                TextAlignmentOptions.TopLeft, new Color(0.95f, 0.98f, 1f, 0.98f), 9f, 12.2f);
            _rulesText.margin = new Vector4(10f, 9f, 10f, 9f);
            _rulesText.lineSpacing = -2f;
            _rulesText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void BuildCompactLayout(RectTransform rectTransform)
        {
            _frame = CreateImage("Frame", rectTransform, new Color(0f, 0f, 0f, 0.18f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 88f));

            _moduleIconImage = CreateImage("ModuleIcon", rectTransform, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));
            _moduleIconImage.preserveAspect = true;
        }

        private void ApplyCard()
        {
            _baseColor = GetTierColor(_card.Tier);
            _background.color = _visualMode == CardVisualMode.Compact
                ? new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.92f)
                : _baseColor;

            _frame.color = Tint(_baseColor, -0.08f);
            if (_topBand != null)
                _topBand.color = Tint(_baseColor, -0.18f);

            if (_visualMode == CardVisualMode.Compact)
                ApplyCompactCard();
            else
                ApplyStandardCard();
        }

        private void ApplyStandardCard()
        {
            if (_rulesPanel != null)
                _rulesPanel.color = Tint(_baseColor, -0.24f);

            if (_iconText != null)
                _iconText.text = _card.IconLabel;
            ApplyModuleIcon();
            if (_tierText != null)
                _tierText.text = _card.TierLabel;
            if (_titleText != null)
                _titleText.text = _card.DisplayName;
            if (_diceText != null)
                _diceText.text = _card.DiceLabel;
            if (_diceBadge != null)
                _diceBadge.gameObject.SetActive(_card.UsesDice);
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
            ApplyModuleIcon();

            ApplyArtwork();
        }

        private void ApplyModuleIcon()
        {
            if (_moduleIconImage == null)
                return;

            Sprite icon = _moduleIconSprite != null ? _moduleIconSprite : _card.ArtworkSprite;
            if (icon != null)
            {
                _moduleIconImage.sprite = icon;
                _moduleIconImage.color = Color.white;
                _moduleIconImage.enabled = true;
                _moduleIconImage.gameObject.SetActive(true);

                if (_iconText != null)
                    _iconText.gameObject.SetActive(false);
            }
            else
            {
                _moduleIconImage.sprite = null;
                _moduleIconImage.enabled = false;
                _moduleIconImage.gameObject.SetActive(false);

                if (_iconText != null)
                    _iconText.gameObject.SetActive(true);
            }
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

            var baseBlock = $"<color=#F0D27A><b>{card.BaseRulesTitle}</b></color>\n{card.BaseRulesText}";
            if (!card.HasPassive)
                return baseBlock;

            return $"{baseBlock}\n\n<color=#F0D27A><b>{card.PassiveName}:</b></color>\n{card.PassiveDescription}";
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
                    return new Color(0.31f, 0.53f, 0.25f, 1f);
                case IronTideCardTier.Tier2:
                    return new Color(0.43f, 0.40f, 0.65f, 1f);
                case IronTideCardTier.Legendary:
                    return new Color(0.64f, 0.49f, 0.17f, 1f);
                case IronTideCardTier.Epic:
                    return new Color(0.55f, 0.25f, 0.36f, 1f);
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
