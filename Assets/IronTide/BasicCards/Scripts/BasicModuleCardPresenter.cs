using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using UnityEngine;

namespace IronTide.BasicCards
{
    [ExecuteAlways]
    [RequireComponent(typeof(CardUI))]
    public sealed class BasicModuleCardPresenter : MonoBehaviour
    {
        [SerializeField] private BasicModuleType moduleType;
        [SerializeField] private int buyCost = 3;
        [SerializeField] private int sellValue = 2;
        [SerializeField] private CardData cardData;

        private CardUI _cardUi;

        public BasicModuleType ModuleType => moduleType;
        public int BuyCost => buyCost;
        public int SellValue => sellValue;
        public CardData CardData => cardData;

        private void Awake()
        {
            ApplyCard();
        }

        private void OnEnable()
        {
            ApplyCard();
        }

        private void OnValidate()
        {
            ApplyCard();
        }

        [ContextMenu("Apply Card Data")]
        public void ApplyCard()
        {
            if (cardData == null)
                return;

            if (_cardUi == null)
                _cardUi = GetComponent<CardUI>();

            if (_cardUi == null)
                return;

            cardData.UpdateDescription();
            _cardUi.SetCard(cardData, false);
        }
    }
}
