using UnityEngine;
using IronTide.BasicCards;
using System.Collections.Generic;

public class ShipInfo : MonoBehaviour
{
    public const int HealthPerModule = 10;

    public struct WeaponDamageRoll
    {
        public int DiceTotal;
        public int BonusTotal;
        public int TotalDamage;
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
        int totalDamage = Mathf.Max(0, damage - GetDamageReduction(rangeModifier, coverModifier));
        if (totalDamage <= 0 || Sunk)
            return 0;

        int damageToNextModuleBreak = GetDamageToNextModuleBreak();
        int appliedDamage = Mathf.Min(totalDamage, damageToNextModuleBreak);
        Health = Mathf.Max(0, Health - appliedDamage);

        if (ShouldCheatDeath(appliedDamage))
        {
            Health = Mathf.Min(1, MaxHealth);
            cheatDeathUsed = true;
            OnModuleStateChanged?.Invoke();
            return appliedDamage;
        }

        if (Health % HealthPerModule == 0)
        {
            DestroyModule();

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

        OnModuleStateChanged?.Invoke();
        return appliedDamage;
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
                    WeaponModule.PassiveKey == "perfect_shot_t2" ||
                    WeaponModule.PassiveKey == "lucky")
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

        if (HasActivePassive(WeaponModule, "marauder") && Health <= 5)
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

        if (WeaponModule != null && WeaponModule.IsValid)
        {
            var rolls = new List<int>();
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
        }

        if (HasActivePassive(EngineModule, "sea_begger"))
            bonusDamage += 1;

        if (HasActivePassive(WeaponModule, "marauder") && Health <= 5)
            bonusDamage += 3;

        if (target != null && IsHighestHealthTarget(target))
        {
            if (HasActivePassive(WeaponModule, "healthy_collector_t1"))
                bonusDamage += 1;
            else if (HasActivePassive(WeaponModule, "healthy_collector_t2"))
                bonusDamage += 2;
        }

        int total = Mathf.Max(0, diceDamage + bonusDamage);
        return new WeaponDamageRoll
        {
            DiceTotal = diceDamage,
            BonusTotal = bonusDamage,
            TotalDamage = total
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

            if (weaponEnabled && WeaponModule.PassiveKey == "long_shot")
                range = 7;

            if (weaponEnabled && WeaponModule.PassiveKey == "sea_horse")
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
        { 6, 2 }
    };

    //public int GetMoveDistance(bool addBonus)
    //{
    //    DiceComponent myDice = dice != null ? dice : GetComponent<DiceComponent>();
    //    if (myDice == null)
    //    {
    //        Debug.LogWarning($"{gameObject.name} is missing a DiceComponent.");
    //        return 0;
    //    }

    //    if (HasActivePassive(ArmorModule, "king_of_the_sea"))
    //    {
    //        Debug.Log($"{gameObject.name} movement fixed to 2 by King of the Sea.");
    //        return 2;
    //    }

    //    int value = RollEngineDice(myDice);
    //    bool secondMove = TurnManager.Instance != null && TurnManager.Instance.MovesUsedthisTurn > 0;
    //    bool canUseBonus = addBonus && (!secondMove || HasActivePassive(EngineModule, "momentum"));

    //    int bonus = canUseBonus && engineEnabled && EngineModule != null && EngineModule.IsValid
    //        ? EngineModule.BaseModifier
    //        : 0;

    //    int total = Mathf.Max(0, value + bonus);
    //    Debug.Log($"{gameObject.name} rolled {value} for movement. Engine bonus {bonus}. Total {total}.");
    //    return total;
    //}

    public int CalculateMoveDistance(int rolledValue, bool addBonus)
    {
        if (HasActivePassive(ArmorModule, "king_of_the_sea"))
        {
            return 2;
        }

        bool secondMove = TurnManager.Instance != null && TurnManager.Instance.MovesUsedthisTurn > 0;
        bool canUseBonus = addBonus && (!secondMove || HasActivePassive(EngineModule, "momentum"));

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
        int reduction = GetArmor();

        if (HasActivePassive(ArmorModule, "supplies") && Health == MaxHealth)
            reduction += 2;

        if (HasActivePassive(ArmorModule, "lookout") && rangeModifier > 0)
            reduction += 1;

        if (HasActivePassive(EngineModule, "sneaky") && coverModifier < 0)
            reduction += Mathf.Abs(coverModifier) / 2;

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

        return card.PassiveKey == passiveKey;
    }

    private bool ShouldCheatDeath(int damageTaken)
    {
        return damageTaken > 0 && Health <= 0 && HasActivePassive(ArmorModule, "cheat_death") && !cheatDeathUsed;
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

        if (HasActivePassive(WeaponModule, "lucky") && rolls.Count > 0)
        {
            for (int i = 0; i < rolls.Count; i++)
            {
                if (rolls[i] % 2 == 1)
                {
                    extraDamage += RollDice(WeaponModule.DiceSides);
                    break;
                }
            }
        }

        return extraDamage;
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
