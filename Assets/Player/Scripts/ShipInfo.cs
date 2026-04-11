using UnityEngine;
using IronTide.BasicCards;
using UnityEditor.SceneManagement;
using NueGames.NueDeck.Scripts.NueExtentions;

public class ShipInfo : MonoBehaviour
{
    [SerializeField] private IronTideModuleCardEntry weaponModule;
    [SerializeField] private bool weaponEnabled;

    [SerializeField] private IronTideModuleCardEntry engineModule;
    [SerializeField] private bool engineEnabled;

    [SerializeField] private IronTideModuleCardEntry armorModule;
    [SerializeField] private bool armorEnabled;

    [SerializeField] private int health;
    [SerializeField] private int maxHealth = 10;

    private int defaultWeaponRange = 6;

    private DiceComponent dice;

    private void Awake()
    {
        dice = this.AddComponent<DiceComponent>();//If diceComponent is made into a singleton this will not be needed
    }

    //Reset void to be called at start of round
    public void ResetValues()
    {
        health = maxHealth;

        if (weaponModule.Id != "")
            weaponEnabled = true;

        if (engineModule.Id != "")
            engineEnabled = true;

        if (armorModule.Id != "")
            armorEnabled = true;
    }

    public void Hurt(int damage)
    {
        health -= damage - GetArmor();
        if (health <= 0)
        {

            DestroyModule();

            if (GetActiveModuleAmount() > 0)
            {
                health = maxHealth;
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
            health = maxHealth;
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

        if (weaponModule.Id == "")
        {
            damage = dice.RollD6();
        }
        else
        {
            for (int i = 0; i < weaponModule.DiceCount; i++)
            {
                damage += dice.RollDice(weaponModule.DiceSides);
            }

            if (weaponEnabled)
                damage += weaponModule.BaseModifier;

        }

        return damage;
    }

    public int GetWeaponRange()
    {
        if (weaponModule.Id != "")
        {
            return defaultWeaponRange;
        }
        else
        {
            return 6; //*REPLACE WITH* Weapon range;
        }

    }

    public int GetMoveDistance(bool addBonus)
    {
        int distance = 0;

        if (engineModule.Id == "")
        {
            distance = dice.RollD6();
        }
        else
        {
            for (int i = 0; i < engineModule.DiceCount; i++)
            {
                distance += dice.RollDice(engineModule.DiceSides);
            }

            if (addBonus && engineEnabled)
                distance += engineModule.BaseModifier;
        }

        return distance;
    }

    public int GetArmor()
    {
        int armor = 0;

        if (armorModule != null && armorEnabled)
        {
            armor = armorModule.BaseModifier;
        }

        return armor;
    }
}
