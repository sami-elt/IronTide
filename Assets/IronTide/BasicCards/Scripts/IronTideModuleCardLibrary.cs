using System;
using System.Collections.Generic;
using UnityEngine;

namespace IronTide.BasicCards
{
    public enum IronTideCardTier
    {
        Tier1 = 0,
        Tier2 = 1,
        Legendary = 2,
        Epic = 3
    }

    public enum IronTideModuleArchetype
    {
        LongRangeWeapon = 0,
        MediumRangeWeapon = 1,
        ShortRangeWeapon = 2,
        Armor = 3,
        Engine = 4
    }

    [Serializable]
    public sealed class IronTideModuleCardEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private IronTideModuleArchetype archetype;
        [SerializeField] private IronTideCardTier tier;
        [SerializeField] private int baseModifier;
        [SerializeField] private int diceCount;
        [SerializeField] private int diceSides;
        [SerializeField] private string passiveKey;
        [SerializeField] private string passiveName;
        [TextArea(2, 5)] [SerializeField] private string passiveDescription;
        [SerializeField] private Sprite artworkSprite;

        public string Id => id;
        public string DisplayName => displayName;
        public IronTideModuleArchetype Archetype => archetype;
        public IronTideCardTier Tier => tier;
        public int BaseModifier => baseModifier;
        public int DiceCount => diceCount;
        public int DiceSides => diceSides;
        public string PassiveKey => passiveKey;
        public string PassiveName => passiveName;
        public string PassiveDescription => passiveDescription;
        public Sprite ArtworkSprite => artworkSprite;

        public bool IsValid => !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(displayName);
        public bool HasPassive => !string.IsNullOrWhiteSpace(passiveName) && passiveName != "-";
        public bool UsesDice => diceCount > 0 && diceSides > 0;
        public bool AppearsInBasicShop => tier == IronTideCardTier.Tier1;
        public bool AppearsInAdvancedShop => tier == IronTideCardTier.Epic || tier == IronTideCardTier.Legendary;
        public bool IsLegendary => tier == IronTideCardTier.Legendary;
        public bool IsEpic => tier == IronTideCardTier.Epic;

        public BasicModuleType SlotType
        {
            get
            {
                switch (archetype)
                {
                    case IronTideModuleArchetype.Armor:
                        return BasicModuleType.Armor;
                    case IronTideModuleArchetype.Engine:
                        return BasicModuleType.Engine;
                    default:
                        return BasicModuleType.Weapon;
                }
            }
        }

        public int BuyCost
        {
            get
            {
                switch (tier)
                {
                    case IronTideCardTier.Tier1:
                        return 5;
                    case IronTideCardTier.Tier2:
                        return 10;
                    case IronTideCardTier.Epic:
                        return 10;
                    case IronTideCardTier.Legendary:
                        return 20;
                    default:
                        return 0;
                }
            }
        }

        public int SellValue
        {
            get
            {
                switch (tier)
                {
                    case IronTideCardTier.Tier1:
                        return 2;
                    case IronTideCardTier.Tier2:
                    case IronTideCardTier.Legendary:
                    case IronTideCardTier.Epic:
                        return 5;
                    default:
                        return 0;
                }
            }
        }

        public string TierLabel
        {
            get
            {
                switch (tier)
                {
                    case IronTideCardTier.Tier1:
                        return "Tier 1";
                    case IronTideCardTier.Tier2:
                        return "Tier 2";
                    case IronTideCardTier.Legendary:
                        return "Legendary";
                    case IronTideCardTier.Epic:
                        return "Epic";
                    default:
                        return "Unknown";
                }
            }
        }

        public string ModifierLabel => baseModifier >= 0 ? $"+{baseModifier}" : baseModifier.ToString();
        public string DiceLabel => UsesDice ? $"X{diceCount}\nD{diceSides}" : "-";

        public string IconLabel
        {
            get
            {
                switch (archetype)
                {
                    case IronTideModuleArchetype.LongRangeWeapon:
                        return "LR";
                    case IronTideModuleArchetype.MediumRangeWeapon:
                        return "MR";
                    case IronTideModuleArchetype.ShortRangeWeapon:
                        return "SR";
                    case IronTideModuleArchetype.Armor:
                        return "AR";
                    case IronTideModuleArchetype.Engine:
                        return "EN";
                    default:
                        return "?";
                }
            }
        }

        public string HeaderLabel
        {
            get
            {
                switch (archetype)
                {
                    case IronTideModuleArchetype.LongRangeWeapon:
                        return "Long Range";
                    case IronTideModuleArchetype.MediumRangeWeapon:
                        return "Medium Range";
                    case IronTideModuleArchetype.ShortRangeWeapon:
                        return "Short Range";
                    case IronTideModuleArchetype.Armor:
                        return "Armor";
                    case IronTideModuleArchetype.Engine:
                        return "Engine";
                    default:
                        return "Module";
                }
            }
        }

        public string ArchetypeLabel
        {
            get
            {
                switch (archetype)
                {
                    case IronTideModuleArchetype.LongRangeWeapon:
                        return "Long Range Weapon";
                    case IronTideModuleArchetype.MediumRangeWeapon:
                        return "Medium Range Weapon";
                    case IronTideModuleArchetype.ShortRangeWeapon:
                        return "Short Range Weapon";
                    case IronTideModuleArchetype.Armor:
                        return "Armor";
                    case IronTideModuleArchetype.Engine:
                        return "Engine";
                    default:
                        return "Module";
                }
            }
        }

        public string BaseRulesTitle => $"{HeaderLabel}:";

        public string BaseRulesText
        {
            get
            {
                switch (archetype)
                {
                    case IronTideModuleArchetype.LongRangeWeapon:
                        return "3-4 range +0\n5 range +1\n6 range +2\nRocks: -2 each.";
                    case IronTideModuleArchetype.MediumRangeWeapon:
                        return "1-4 range +0\nNo range bonus or penalty.";
                    case IronTideModuleArchetype.ShortRangeWeapon:
                        return "1 range +2\n2 range +0\n3 range -1\n4 range -2\nOptional knockback 1.";
                    case IronTideModuleArchetype.Armor:
                        return "Mitigates incoming damage by its armor value.";
                    case IronTideModuleArchetype.Engine:
                        return "Move = 1xD6 + modifier.\nExtra move uses no modifier.";
                    default:
                        return string.Empty;
                }
            }
        }

        public string ArtPlaceholderLabel
        {
            get
            {
                switch (archetype)
                {
                    case IronTideModuleArchetype.LongRangeWeapon:
                        return "LONG\nWEAPON";
                    case IronTideModuleArchetype.MediumRangeWeapon:
                        return "MED\nWEAPON";
                    case IronTideModuleArchetype.ShortRangeWeapon:
                        return "SHORT\nWEAPON";
                    case IronTideModuleArchetype.Armor:
                        return "ARMOR";
                    case IronTideModuleArchetype.Engine:
                        return "ENGINE";
                    default:
                        return "CARD";
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "IronTideModuleLibrary", menuName = "Iron Tide/Module Library", order = 0)]
    public sealed class IronTideModuleCardLibrary : ScriptableObject
    {
        [SerializeField] private List<IronTideModuleCardEntry> cards = new List<IronTideModuleCardEntry>();

        public List<IronTideModuleCardEntry> Cards => cards;
    }
}
