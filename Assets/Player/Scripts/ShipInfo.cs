using UnityEngine;
using IronTide.BasicCards;
using System.Collections.Generic;

public class ShipInfo : MonoBehaviour
{
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
    public int MaxHealth { get; } = 10;

    public bool Sunk { get; private set; }

    private readonly int defaultWeaponRange = 4;

    private DiceComponent dice;

    private void Awake()
    {
        dice = GetComponent<DiceComponent>();
        if (dice == null)
            dice = gameObject.AddComponent<DiceComponent>();//If diceComponent is made into a singleton this will not be needed
    }

    //Reset void to be called at start of round
    public void ResetValues()
    {
        Health = MaxHealth;
        SetSunk(false);

        if (WeaponModule != null && WeaponModule.IsValid)
            weaponEnabled = true;

        if (EngineModule != null && EngineModule.IsValid)
            engineEnabled = true;

        if (ArmorModule != null && ArmorModule.IsValid)
            armorEnabled = true;

        OnModuleStateChanged?.Invoke();
    }

    public void Hurt(int damage)
    {
        int totalDamage = damage - GetArmor();
        if (totalDamage > 0)
            Health -= totalDamage;

        if (Health <= 0)
        {

            DestroyModule();

            if (GetActiveModuleAmount() > 0)
            {
                Health = MaxHealth;
            }
            else
            {
                SetSunk(true);
            }

        }
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
            int destroyRoll = dice.RollD4();
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

    public int GetWeaponDamage()
    {
        int damage = 0;

        if (WeaponModule != null && WeaponModule.IsValid)
        {
            for (int i = 0; i < WeaponModule.DiceCount; i++)
            {
                damage += dice.RollDice(WeaponModule.DiceSides);
            }

            if (weaponEnabled)
                damage += WeaponModule.BaseModifier;
        }
        else
        {
            damage = dice.RollD6();
        }

        return damage;
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

    public int GetMoveDistance(bool addBonus)
    {
        DiceComponent myDice = dice != null ? dice : GetComponent<DiceComponent>();
        if (myDice == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing a DiceComponent.");
            return 0;
        }

        int value = myDice.RollD6();
        int bonus = addBonus && engineEnabled && EngineModule != null && EngineModule.IsValid
            ? EngineModule.BaseModifier
            : 0;

        int total = Mathf.Max(0, value + bonus);
        Debug.Log($"{gameObject.name} rolled {value} for movement. Engine bonus {bonus}. Total {total}.");
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

}
