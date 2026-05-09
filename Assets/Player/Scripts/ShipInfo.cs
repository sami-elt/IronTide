using UnityEngine;
using IronTide.BasicCards;
using NueGames.NueDeck.Scripts.NueExtentions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class ShipInfo : MonoBehaviour
{
    public IronTideModuleCardEntry WeaponModule { get; private set; }
    [SerializeField] private bool weaponEnabled;

    public IronTideModuleCardEntry EngineModule { get; private set; }
    [SerializeField] private bool engineEnabled;

    public IronTideModuleCardEntry ArmorModule { get; private set; }
    [SerializeField] private bool armorEnabled;

    public int Health { get; private set; }
    public int MaxHealth { get; } = 10;

    public bool Sunk { get; private set; }

    private readonly int defaultWeaponRange = 4;

    private DiceComponent dice;

    private void Awake()
    {
        dice = this.AddComponent<DiceComponent>();//If diceComponent is made into a singleton this will not be needed
    }

    //Reset void to be called at start of round
    public void ResetValues()
    {
        Health = MaxHealth;
        SetSunk(false);

        if (WeaponModule != null && WeaponModule.Id != "")
            weaponEnabled = true;

        if (EngineModule != null && EngineModule.Id != "")
            engineEnabled = true;

        if (ArmorModule != null && ArmorModule.Id != "")
            armorEnabled = true;
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

            if (destroyRoll == 1 && weaponEnabled)
            {
                weaponEnabled = false;
            }
            else if (destroyRoll == 2 && engineEnabled)
            {
                engineEnabled = false;
            }
            else if (destroyRoll == 3 && armorEnabled)
            {
                armorEnabled = false;
            }
            else
            {
                //Allow for choice by attacker of destroyed module out of the current ones active
                //For now it will just destroy the weapon!!!

                weaponEnabled = false;
            }
        }
    }

    private void SetSunk(bool value)
    {
        Sunk = value;

        gameObject.SetActive(!Sunk);
    }

    public void SetWeaponModule(IronTideModuleCardEntry weaponModule)
    {
        IronTideModuleArchetype archetype = weaponModule.Archetype;

        if (weaponModule == null)
        {
            WeaponModule = null;
            return;
        }

        bool isWeapon = archetype == IronTideModuleArchetype.LongRangeWeapon ||
            archetype == IronTideModuleArchetype.MediumRangeWeapon ||
            archetype == IronTideModuleArchetype.ShortRangeWeapon;

        if (isWeapon)
        {
            WeaponModule = weaponModule;
            return;
        }

        Debug.LogWarning($"Module attempted to set as weapon for {this} is not a module of any weapon archetype and remains unchanged.");
    }

    public void SetEngineModule(IronTideModuleCardEntry engineModule)
    {
        IronTideModuleArchetype archetype = engineModule.Archetype;

        if (engineModule == null)
        {
            EngineModule = null;
            return;
        }

        bool isEngine = archetype == IronTideModuleArchetype.Engine;

        if (isEngine)
        {
            EngineModule = engineModule;
            return;
        }

        Debug.LogWarning($"Module attempted to set as engine for {this} is not a module of the engine archetype and remains unchanged.");
    }

    public void SetArmorModule(IronTideModuleCardEntry armorModule)
    {
        IronTideModuleArchetype archetype = armorModule.Archetype;

        if (armorModule == null)
        {
            ArmorModule = null;
            return;
        }

        bool isArmor = archetype == IronTideModuleArchetype.Armor;

        if (isArmor)
        {
            ArmorModule = armorModule;
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

        if (WeaponModule != null && WeaponModule.Id != "")
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
        if (WeaponModule != null && WeaponModule.Id != "")
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
        if (WeaponModule == null)
        {
            return 0;
        }
        else if (WeaponModule.Archetype == IronTideModuleArchetype.ShortRangeWeapon)
        {
            return ShortRangeModifiers[distance];
        }
        else if (WeaponModule.Archetype == IronTideModuleArchetype.LongRangeWeapon)
        {
            return LongRangeModifiers[distance];
        }
        else return 0;
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

        DiceComponent myDice = GetComponent<DiceComponent>();

        if (myDice != null)
        {
            int value = myDice.RollD6();

            Debug.Log(gameObject.name + "rullade " + value);
            
            return value;
        }
        else
        {
            Debug.Log("saknar dice");
            return 0;
        }

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