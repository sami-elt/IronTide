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
        private static readonly HashSet<string> DisabledShopPassiveKeys = new HashSet<string>
        {
            "one_of_each_t1",
            "one_of_each",
            "strong_arm_t1",
            "strong_arm",
            "doubleshot_t1",
            "gust_of_wind_t1",
            "gust_of_wind",
            "runaway_t1",
            "runaway",
            "lead_shell_t2",
            "lead_shell",
            "retribution_t2",
            "retribution",
            "doubleshot_t2",
            "hunker_down_t2",
            "hunker_down",
            "scavanger_t2",
            "scavanger",
            "lucky_legendary",
            "lucky",
            "precision_legendary",
            "precision",
            "coordination_legendary",
            "coordination",
            "surprise_mother_trucker_legendary",
            "surprise_mother_trucker",
            "wolf_pack_legendary",
            "wolf_pack",
            "hard_shell_epic",
            "hard_shell",
            "tactical_retreat_epic",
            "tactical_retreat"
        };

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
        public bool IsPlayableInShop => string.IsNullOrWhiteSpace(passiveKey) || !DisabledShopPassiveKeys.Contains(passiveKey);
        public bool AppearsInBasicShop => tier == IronTideCardTier.Tier1 && IsPlayableInShop;
        public bool AppearsInAdvancedShop => IsPlayableInShop && (tier == IronTideCardTier.Tier2 || tier == IronTideCardTier.Epic || tier == IronTideCardTier.Legendary);
        public bool IsTier2 => tier == IronTideCardTier.Tier2;
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

        public IronTideModuleCardEntry FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            id = NormalizeCardId(id);

            foreach (IronTideModuleCardEntry card in cards)
            {
                if (card != null && card.Id == id)
                    return card;
            }

            return null;
        }

        private static string NormalizeCardId(string id)
        {
            switch (id)
            {
                case "t1_long_deck_gun":
                    return "t1_long_base";
                case "t1_medium_broadside":
                    return "t1_medium_base";
                case "t1_short_boarding_gun":
                    return "t1_short_base";
                case "t1_armor_iron_plating":
                    return "t1_armor_base";
                case "t1_armor_lookout":
                    return "t1_armor_look_out";
                case "t1_engine_full_steam":
                    return "t1_engine_base";
                case "t2_long_heavy_deck_gun":
                    return "t2_long_base";
                case "t2_medium_heavy_broadside":
                    return "t2_medium_base";
                case "t2_short_breaching_gun":
                    return "t2_short_base";
                case "t2_armor_fortified_hull":
                    return "t2_armor_base";
                case "t2_engine_twin_engines":
                    return "t2_engine_base";
                default:
                    return id;
            }
        }

        public List<IronTideModuleCardEntry> GetTier1Cards(BasicModuleType slotType)
        {
            var results = new List<IronTideModuleCardEntry>();
            foreach (IronTideModuleCardEntry card in cards)
            {
                if (card != null && card.IsValid && card.IsPlayableInShop &&
                    card.Tier == IronTideCardTier.Tier1 && card.SlotType == slotType)
                {
                    results.Add(card);
                }
            }

            return results;
        }
    }
}
