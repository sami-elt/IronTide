using UnityEngine;
using IronTide.BasicCards;
using System.Collections.Generic;

public enum IronTideAttackLineType
{
    Unknown = 0,
    Straight = 1,
    Diagonal = 2
}

public class ShipInfo : MonoBehaviour
{
    public const int HealthPerModule = 10;

    public struct WeaponDamageRoll
    {
        public int DiceTotal;
        public int BonusTotal;
        public int TotalDamage;
        public int DiceSides;
        public bool RolledCrit;
    }

    public struct DamageResult
    {
        public int RawDamage;
        public int DamageReduction;
        public int EffectiveDamage;
        public int DamageDealt;
        public bool ModuleDestroyed;
        public bool Sunk;
        public bool WasCancelled;
    }

    public IronTideModuleCardEntry WeaponModule { get; private set; }
    [SerializeField] private bool weaponEnabled;
    public bool WeaponEnabled => weaponEnabled;

    public IronTideModuleCardEntry EngineModule { get; private set; }
    [SerializeField] private bool engineEnabled;
    public bool EngineEnabled => engineEnabled;

    public IronTideModuleCardEntry ArmorModule { get; private set; }
    [SerializeField] private bool armorEnabled;
    public bool ArmorEnabled => armorEnabled;

    public event System.Action OnModuleStateChanged;

    public int Health { get; private set; }
    public int MaxHealth => GetActiveModuleAmount() * HealthPerModule;

    public bool Sunk { get; private set; }

    private readonly int defaultWeaponRange = 4;

    private bool cheatDeathUsed;

    //Fixing so the dice will calculate movement right away.
    private int RollDice(int sides)
    {
        return Random.Range(1, sides + 1);
    }
    private void Awake()
    {
    }

    //Reset void to be called at start of round
    public void ResetValues()
    {
        cheatDeathUsed = false;

        weaponEnabled = WeaponModule != null && WeaponModule.IsValid;
        engineEnabled = EngineModule != null && EngineModule.IsValid;
        armorEnabled = ArmorModule != null && ArmorModule.IsValid;

        Health = MaxHealth;
        SetSunk(Health <= 0);

        OnModuleStateChanged?.Invoke();
    }

    public int Hurt(int damage)
    {
        return Hurt(damage, 0, 0);
    }

    public int Hurt(int damage, int rangeModifier, int coverModifier)
    {
        return HurtDetailed(damage, null, rangeModifier, coverModifier, IronTideAttackLineType.Unknown, false).DamageDealt;
    }

    public DamageResult HurtDetailed(int damage, ShipInfo attacker, int rangeModifier, int coverModifier,
        IronTideAttackLineType lineType, bool allowOverkill)
    {
        var result = new DamageResult
        {
            RawDamage = Mathf.Max(0, damage),
            DamageReduction = GetDamageReduction(attacker, rangeModifier, coverModifier, lineType)
        };

        result.EffectiveDamage = Mathf.Max(0, result.RawDamage - result.DamageReduction);
        if (result.EffectiveDamage <= 0 || Sunk)
            return result;

        int damageToNextModuleBreak = GetDamageToNextModuleBreak();
        int appliedDamage = Mathf.Min(result.EffectiveDamage, damageToNextModuleBreak);
        ApplyHealthDamage(appliedDamage, ref result);

        int overflowDamage = Mathf.Max(0, result.EffectiveDamage - appliedDamage);
        if (allowOverkill && overflowDamage > 0 && result.ModuleDestroyed && !Sunk)
        {
            int carryDamage = overflowDamage / 2;
            if (carryDamage > 0)
                ApplyHealthDamage(carryDamage, ref result);
        }

        result.Sunk = Sunk;
        OnModuleStateChanged?.Invoke();
        return result;
    }

    public DamageResult LoseHealthDirect(int amount, ShipInfo source)
    {
        var result = new DamageResult
        {
            RawDamage = Mathf.Max(0, amount),
            EffectiveDamage = Mathf.Max(0, amount)
        };

        if (result.EffectiveDamage <= 0 || Sunk)
            return result;

        ApplyHealthDamage(result.EffectiveDamage, ref result);
        result.Sunk = Sunk;
        OnModuleStateChanged?.Invoke();
        return result;
    }

    private void ApplyHealthDamage(int amount, ref DamageResult result)
    {
        if (amount <= 0 || Sunk)
            return;

        int damageToNextModuleBreak = GetDamageToNextModuleBreak();
        int appliedDamage = Mathf.Min(amount, damageToNextModuleBreak);
        Health = Mathf.Max(0, Health - appliedDamage);
        result.DamageDealt += appliedDamage;

        if (ShouldCheatDeath(appliedDamage))
        {
            Health = Mathf.Min(1, MaxHealth);
            cheatDeathUsed = true;
            return;
        }

        if (Health % HealthPerModule != 0)
            return;

        DestroyModule();
        result.ModuleDestroyed = true;

        if (GetActiveModuleAmount() > 0)
        {
            Health = Mathf.Min(Health, MaxHealth);
        }
        else
        {
            Health = 0;
            SetSunk(true);
        }
    }

    private int GetDamageToNextModuleBreak()
    {
        if (Health <= 0)
            return 0;

        int previousLine = ((Health - 1) / HealthPerModule) * HealthPerModule;
        return Mathf.Max(1, Health - previousLine);
    }

    private void DestroyModule()
    {
        if (GetActiveModuleAmount() == 0)
            return;

        if (GetActiveModuleAmount() == 1)
        {
            //Find module that is active and deactivate it
            if (weaponEnabled)
            {
                weaponEnabled = false;
            }
            else if (engineEnabled)
            {
                engineEnabled = false;
            }
            else if (armorEnabled)
            {
                armorEnabled = false;
            }
        }
        else
        {
            int destroyRoll = RollDice(4);
            bool moduleDestroyed = false;

            if (destroyRoll == 1 && weaponEnabled)
            {
                weaponEnabled = false;
                moduleDestroyed = true;
            }
            else if (destroyRoll == 2 && armorEnabled)
            {
                armorEnabled = false;
                moduleDestroyed = true;
            }
            else if (destroyRoll == 3 && engineEnabled)
            {
                engineEnabled = false;
                moduleDestroyed = true;
            }

            if (!moduleDestroyed)
            {
                DestroyRandomActiveModule();
            }
        }

        OnModuleStateChanged?.Invoke();
    }

    private void DestroyRandomActiveModule()
    {
        var activeSlots = new List<int>(3);
        if (weaponEnabled)
            activeSlots.Add(1);
        if (armorEnabled)
            activeSlots.Add(2);
        if (engineEnabled)
            activeSlots.Add(3);

        if (activeSlots.Count == 0)
            return;

        int selectedSlot = activeSlots[Random.Range(0, activeSlots.Count)];
        if (selectedSlot == 1)
            weaponEnabled = false;
        else if (selectedSlot == 2)
            armorEnabled = false;
        else
            engineEnabled = false;
    }

    private void SetSunk(bool value)
    {
        Sunk = value;

        gameObject.SetActive(!Sunk);
    }

    public void SetWeaponModule(IronTideModuleCardEntry weaponModule)
    {
        if (weaponModule == null)
        {
            WeaponModule = null;
            weaponEnabled = false;
            OnModuleStateChanged?.Invoke();
            return;
        }

        IronTideModuleArchetype archetype = weaponModule.Archetype;
        bool isWeapon = archetype == IronTideModuleArchetype.LongRangeWeapon ||
            archetype == IronTideModuleArchetype.MediumRangeWeapon ||
            archetype == IronTideModuleArchetype.ShortRangeWeapon;

        if (isWeapon)
        {
            WeaponModule = weaponModule;
            weaponEnabled = weaponModule.IsValid;
            RefreshAfterModuleAssigned();
            OnModuleStateChanged?.Invoke();
            return;
        }

        Debug.LogWarning($"Module attempted to set as weapon for {this} is not a module of any weapon archetype and remains unchanged.");
    }

    public void SetEngineModule(IronTideModuleCardEntry engineModule)
    {
        if (engineModule == null)
        {
            EngineModule = null;
            engineEnabled = false;
            OnModuleStateChanged?.Invoke();
            return;
        }

        IronTideModuleArchetype archetype = engineModule.Archetype;
        bool isEngine = archetype == IronTideModuleArchetype.Engine;

        if (isEngine)
        {
            EngineModule = engineModule;
            engineEnabled = engineModule.IsValid;
            RefreshAfterModuleAssigned();
            OnModuleStateChanged?.Invoke();
            return;
        }

        Debug.LogWarning($"Module attempted to set as engine for {this} is not a module of the engine archetype and remains unchanged.");
    }

    public void SetArmorModule(IronTideModuleCardEntry armorModule)
    {
        if (armorModule == null)
        {
            ArmorModule = null;
            armorEnabled = false;
            OnModuleStateChanged?.Invoke();
            return;
        }

        IronTideModuleArchetype archetype = armorModule.Archetype;
        bool isArmor = archetype == IronTideModuleArchetype.Armor;

        if (isArmor)
        {
            ArmorModule = armorModule;
            armorEnabled = armorModule.IsValid;
            RefreshAfterModuleAssigned();
            OnModuleStateChanged?.Invoke();
            return;
        }

        Debug.LogWarning($"Module attempted to set as armor for {this} is not a module of the armor archetype and remains unchanged.");
    }


    public int GetActiveModuleAmount()
    {
        int amount = 0;

        if (weaponEnabled)
            amount++;

        if (engineEnabled)
            amount++;

        if (armorEnabled)
            amount++;

        return amount;
    }

    private void RefreshAfterModuleAssigned()
    {
        if (MaxHealth <= 0)
            return;

        if (Sunk)
            SetSunk(false);

        if (Health <= 0)
            Health = MaxHealth;
        else if (Health > MaxHealth)
            Health = MaxHealth;
    }

    public int GetWeaponDamage()
    {
        return GetWeaponDamage(null);
    }

    public void GetWeaponDamageRange(out int minDamage, out int maxDamage)
    {
        if (WeaponModule != null && WeaponModule.IsValid && WeaponModule.UsesDice)
        {
            minDamage = WeaponModule.DiceCount;
            maxDamage = WeaponModule.DiceCount * WeaponModule.DiceSides;

            if (weaponEnabled)
            {
                minDamage += WeaponModule.BaseModifier;
                maxDamage += WeaponModule.BaseModifier;

                if (WeaponModule.PassiveKey == "perfect_shot_t1" ||
                    WeaponModule.PassiveKey == "perfect_shot_t2")
                {
                    maxDamage += WeaponModule.DiceSides;
                }
            }
        }
        else
        {
            minDamage = 1;
            maxDamage = 6;
        }

        if (HasActivePassive(WeaponModule, "marauder_epic") && Health <= 5)
        {
            minDamage += 3;
            maxDamage += 3;
        }

        minDamage = Mathf.Max(0, minDamage);
        maxDamage = Mathf.Max(minDamage, maxDamage);
    }

    public int GetWeaponDamage(ShipInfo target)
    {
        return RollWeaponDamage(target).TotalDamage;
    }

    public WeaponDamageRoll RollWeaponDamage(ShipInfo target)
    {
        int diceDamage = 0;
        int bonusDamage = 0;
        var rolls = new List<int>();

        if (WeaponModule != null && WeaponModule.IsValid)
        {
            for (int i = 0; i < WeaponModule.DiceCount; i++)
            {
                int roll = RollDice(WeaponModule.DiceSides);
                rolls.Add(roll);
                diceDamage += roll;
            }

            if (weaponEnabled)
                bonusDamage += GetExtraWeaponDiceDamage(rolls);

            if (weaponEnabled)
                bonusDamage += WeaponModule.BaseModifier;
        }
        else
        {
            diceDamage = RollDice(6);
            rolls.Add(diceDamage);
        }

        if (HasActivePassive(EngineModule, "sea_begger_epic"))
            bonusDamage += 1;

        if (HasActivePassive(WeaponModule, "marauder_epic") && Health <= 5)
            bonusDamage += 3;

        int total = Mathf.Max(0, diceDamage + bonusDamage);
        return new WeaponDamageRoll
        {
            DiceTotal = diceDamage,
            BonusTotal = bonusDamage,
            TotalDamage = total,
            DiceSides = WeaponModule != null && WeaponModule.IsValid ? WeaponModule.DiceSides : 6,
            RolledCrit = DidRollCrit(rolls, WeaponModule != null && WeaponModule.IsValid ? WeaponModule.DiceSides : 6, diceDamage)
        };
    }

    public int GetWeaponRange()
    {
        if (WeaponModule != null && WeaponModule.IsValid)
        {
            int range = defaultWeaponRange;

            switch (WeaponModule.Archetype)
            {
                case IronTideModuleArchetype.LongRangeWeapon:
                    range = 6;
                    break;

                case IronTideModuleArchetype.MediumRangeWeapon:
                    range = 4;
                    break;

                case IronTideModuleArchetype.ShortRangeWeapon:
                    range = 4;
                    break;
            }

            if (weaponEnabled && HasActivePassive(WeaponModule, "long_shot_t1"))
                range = 7;

            if (weaponEnabled && HasActivePassive(WeaponModule, "sea_horse_t2"))
                range = 2;

            return range;
        }
        else
        {
            return defaultWeaponRange;
        }

    }

    public int GetDistanceDamageModifier(int distance)
    {
        if (WeaponModule == null || !WeaponModule.IsValid)
        {
            return 0;
        }

        if (WeaponModule.Archetype == IronTideModuleArchetype.ShortRangeWeapon &&
            ShortRangeModifiers.TryGetValue(distance, out int shortRangeModifier))
        {
            return shortRangeModifier;
        }

        if (WeaponModule.Archetype == IronTideModuleArchetype.LongRangeWeapon &&
            LongRangeModifiers.TryGetValue(distance, out int longRangeModifier))
        {
            return longRangeModifier;
        }

        return 0;
    }

    //Gathered as { Distance, Modifier }
    public static Dictionary<int, int> ShortRangeModifiers { get; } = new Dictionary<int, int>
    {
        { 1, 2 },
        { 2, 0 },
        { 3, -1 },
        { 4, -2 }
    };

    public static Dictionary<int, int> LongRangeModifiers { get; } = new Dictionary<int, int>
    {
        { 1, 0 },
        { 2, 0 },
        { 3, 0 },
        { 4, 0 },
        { 5, 1 },
        { 6, 2 },
        { 7, 0 }
    };

    //public int GetMoveDistance(bool addBonus)
    //{
    //    DiceComponent myDice = dice != null ? dice : GetComponent<DiceComponent>();
    //    if (myDice == null)
    //    {
    //        Debug.LogWarning($"{gameObject.name} is missing a DiceComponent.");
    //        return 0;
    //    }

    public int CalculateMoveDistance(int rolledValue, bool addBonus)
    {
        if (HasActivePassive(ArmorModule, "king_of_the_sea_legendary"))
        {
            return 2;
        }

        bool secondMove = TurnManager.Instance != null && TurnManager.Instance.MovesUsedthisTurn > 0;
        bool canUseBonus = addBonus && (!secondMove || HasActivePassive(EngineModule, "momentum_t1"));

        int bonus = canUseBonus && engineEnabled && EngineModule != null && EngineModule.IsValid
            ? EngineModule.BaseModifier
            : 0;

        int total = Mathf.Max(0, rolledValue + bonus);

        return total;
    }


    public int GetArmor()
    {
        int armor = 0;

        if (ArmorModule != null && armorEnabled)
        {
            armor = ArmorModule.BaseModifier;
        }

        return armor;
    }

    public int GetDamageReduction(int rangeModifier, int coverModifier)
    {
        return GetDamageReduction(null, rangeModifier, coverModifier, IronTideAttackLineType.Unknown);
    }

    public int GetDamageReduction(ShipInfo attacker, int rangeModifier, int coverModifier, IronTideAttackLineType lineType)
    {
        int reduction = GetArmor();

        if (HasActivePassive(ArmorModule, "supplies_t1") && Health == MaxHealth)
            reduction += 2;

        if (HasActivePassive(ArmorModule, "look_out_t1") && rangeModifier > 0)
            reduction += 1;

        if (HasActivePassive(EngineModule, "sneaky_t1"))
            reduction += CountAdjacentRocks();

        if (HasActivePassive(ArmorModule, "rookie_t1") && lineType == IronTideAttackLineType.Straight)
            reduction += 2;

        if (HasActivePassive(ArmorModule, "bishop_armor_t2") && lineType == IronTideAttackLineType.Diagonal)
            reduction += 2;

        if (HasActivePassive(ArmorModule, "hull_of_honor_epic") &&
            attacker != null &&
            TurnManager.Instance != null &&
            !TurnManager.Instance.HasShipAttackedTargetThisRound(this, attacker))
        {
            reduction += 3;
        }

        return Mathf.Max(0, reduction);
    }

    public void ApplyStartTurnEffects()
    {
        if (HasActivePassive(ArmorModule, "scrappy_t1"))
            Heal(1);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || Sunk)
            return;

        Health = Mathf.Min(MaxHealth, Health + amount);
        OnModuleStateChanged?.Invoke();
    }

    public bool HasActivePassive(IronTideModuleCardEntry card, string passiveKey)
    {
        if (card == null || !card.IsValid || string.IsNullOrWhiteSpace(passiveKey))
            return false;

        if (card == WeaponModule && !weaponEnabled)
            return false;

        if (card == ArmorModule && !armorEnabled)
            return false;

        if (card == EngineModule && !engineEnabled)
            return false;

        return NormalizePassiveKey(card.PassiveKey) == NormalizePassiveKey(passiveKey);
    }

    public int GetAttackContextDamageModifier(ShipInfo target, IronTideAttackLineType lineType)
    {
        int modifier = 0;

        if (HasActivePassive(WeaponModule, "bishop_long_t1"))
        {
            if (lineType == IronTideAttackLineType.Diagonal)
                modifier += 2;
            else if (lineType == IronTideAttackLineType.Straight)
                modifier -= 1;
        }

        if (HasActivePassive(WeaponModule, "bishop_long_t2") && lineType == IronTideAttackLineType.Diagonal)
            modifier += 2;

        if (HasActivePassive(WeaponModule, "rook_t1"))
        {
            if (lineType == IronTideAttackLineType.Straight)
                modifier += 2;
            else if (lineType == IronTideAttackLineType.Diagonal)
                modifier -= 1;
        }

        if (HasActivePassive(WeaponModule, "rook_ii_t2") && lineType == IronTideAttackLineType.Straight)
            modifier += 3;

        if (target != null && IsHighestHealthTarget(target))
        {
            if (HasActivePassive(WeaponModule, "healthy_collector_t1"))
                modifier += 1;
            else if (HasActivePassive(WeaponModule, "healthy_collector_ii_t2"))
                modifier += 2;
        }

        if (target != null &&
            HasActivePassive(WeaponModule, "death_of_duty_epic") &&
            TurnManager.Instance != null &&
            !TurnManager.Instance.HasShipAttackedTargetThisRound(this, target))
        {
            modifier += 4;
        }

        return modifier;
    }

    public bool ShouldCancelCriticalAttack(WeaponDamageRoll damageRoll)
    {
        if (!damageRoll.RolledCrit)
            return false;

        if (HasActivePassive(ArmorModule, "dodge_t2") && RollPassiveD6() % 2 == 1)
            return true;

        if (HasActivePassive(EngineModule, "evade_t2") && RollPassiveD6() % 2 == 0)
            return true;

        return false;
    }

    public int RollPassiveD6()
    {
        return RollDice(6);
    }

    private bool ShouldCheatDeath(int damageTaken)
    {
        return damageTaken > 0 && Health <= 0 && HasActivePassive(ArmorModule, "cheat_death_t2") && !cheatDeathUsed;
    }

    //private int RollEngineDice(DiceComponent myDice)
    //{
    //    if (EngineModule == null || !EngineModule.IsValid || !EngineModule.UsesDice)
    //        return myDice.RollD6();

    //    int total = 0;
    //    for (int i = 0; i < EngineModule.DiceCount; i++)
    //        total += myDice.RollDice(EngineModule.DiceSides);

    //    return total;
    //}

    private int GetExtraWeaponDiceDamage(List<int> rolls)
    {
        int extraDamage = 0;

        if (rolls.Count >= 2 && (HasActivePassive(WeaponModule, "perfect_shot_t1") || HasActivePassive(WeaponModule, "perfect_shot_t2")))
        {
            bool allSame = true;
            for (int i = 1; i < rolls.Count; i++)
            {
                if (rolls[i] != rolls[0])
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame)
                extraDamage += RollDice(WeaponModule.DiceSides);
        }

        return extraDamage;
    }

    private static bool DidRollCrit(List<int> rolls, int diceSides, int fallbackRoll)
    {
        if (diceSides <= 0)
            return false;

        if (rolls != null && rolls.Count > 0)
        {
            for (int i = 0; i < rolls.Count; i++)
            {
                if (rolls[i] == diceSides)
                    return true;
            }
        }

        return fallbackRoll == diceSides;
    }

    private int CountAdjacentRocks()
    {
        HexTile[] tiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        int count = 0;
        float adjacentDistance = ShipMovement.DistanceBetweenTiles * 1.45f;

        for (int i = 0; i < tiles.Length; i++)
        {
            HexTile tile = tiles[i];
            if (tile == null || tile.isWalkable)
                continue;

            Vector3 delta = tile.transform.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= adjacentDistance)
                count++;
        }

        return count;
    }

    private static string NormalizePassiveKey(string passiveKey)
    {
        switch (passiveKey)
        {
            case "long_shot":
                return "long_shot_t1";
            case "mortar":
                return "mortar_t1";
            case "one_of_each":
                return "one_of_each_t1";
            case "strong_arm":
                return "strong_arm_t1";
            case "rookie":
                return "rookie_t1";
            case "supplies":
                return "supplies_t1";
            case "lookout":
                return "look_out_t1";
            case "momentum":
                return "momentum_t1";
            case "gust_of_wind":
                return "gust_of_wind_t1";
            case "sneaky":
                return "sneaky_t1";
            case "runaway":
                return "runaway_t1";
            case "piercing_shot":
                return "piercing_shot_t2";
            case "lead_shell":
                return "lead_shell_t2";
            case "retribution":
                return "retribution_t2";
            case "healthy_collector_t2":
                return "healthy_collector_ii_t2";
            case "rook_t2":
                return "rook_ii_t2";
            case "grappling_cannon_t2":
                return "grappling_cannon_ii_t2";
            case "boarding_party_t2":
                return "boarding_party_ii_t2";
            case "sea_horse":
                return "sea_horse_t2";
            case "cheat_death":
                return "cheat_death_t2";
            case "hunker_down":
                return "hunker_down_t2";
            case "dodge":
                return "dodge_t2";
            case "scavanger":
                return "scavanger_t2";
            case "evade":
                return "evade_t2";
            case "ramming_speed":
                return "ramming_speed_t2";
            case "lucky":
                return "lucky_legendary";
            case "precision":
                return "precision_legendary";
            case "coordination":
                return "coordination_legendary";
            case "overkill":
                return "overkill_legendary";
            case "king_of_the_sea":
                return "king_of_the_sea_legendary";
            case "surprise_mother_trucker":
                return "surprise_mother_trucker_legendary";
            case "wolf_pack":
                return "wolf_pack_legendary";
            case "queen_of_the_sea":
                return "queen_of_the_sea_legendary";
            case "marauder":
                return "marauder_epic";
            case "death_of_duty":
                return "death_of_duty_epic";
            case "hard_shell":
                return "hard_shell_epic";
            case "hull_of_honor":
                return "hull_of_honor_epic";
            case "tactical_retreat":
                return "tactical_retreat_epic";
            case "sea_begger":
                return "sea_begger_epic";
            default:
                return passiveKey;
        }
    }

    private bool IsHighestHealthTarget(ShipInfo target)
    {
        if (target == null)
            return false;

        ShipInfo[] allShips = FindObjectsByType<ShipInfo>(FindObjectsSortMode.None);
        int targetScore = target.GetTotalShipHealthScore();

        foreach (ShipInfo shipInfo in allShips)
        {
            if (shipInfo == null || shipInfo == this || shipInfo.Sunk)
                continue;

            if (shipInfo.GetTotalShipHealthScore() > targetScore)
                return false;
        }

        return true;
    }


    //för att få rätt tärning
    public int GetWeaponDiceSides()
    {
        if (WeaponModule != null && WeaponModule.IsValid && WeaponModule.UsesDice)
            return WeaponModule.DiceSides;
        return 6;
    }

    public int GetEngineDice()
    {
        if(EngineModule != null && EngineModule.IsValid && EngineModule.UsesDice)
        {
            return EngineModule.DiceSides;
        }

        //om inget värde returna D6
        return 6;
    }

    private int GetTotalShipHealthScore()
    {
        return Health;
    }

}
