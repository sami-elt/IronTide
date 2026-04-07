using UnityEngine;

public class ShipInfo : MonoBehaviour
{
    //*REPLACE WITH* Reference to current weapon module
    //*REPLACE WITH* Reference to current engine module
    //*REPLACE WITH* Reference to current armor module

    //*REPLACE WITH* Reference to default weapon dice
    //*REPLACE WITH* Reference to default engine dice

    [SerializeField] private int health;
    [SerializeField] private int maxHealth = 10;

    //Reset void to be called at start of round
    public void ResetValues()
    {
        health = maxHealth;

        //*ADD* Set all modules to active
    }

    public void Hurt(int damage)
    {
        health -= damage;
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
        }
        else
        {
            int destroyRoll = Random.Range(1, 5);//*REPLACE WITH* Dice roll of d4

            if (destroyRoll == 1 && true)//Should include a NULL check for weapon module and an active check instead of true
            {
                //Deactivate weapon module
            }
            else if (destroyRoll == 2 && true)//Should include a NULL check for engine module and an active check instead of true
            {
                //Deactivate engine module
            }
            else if (destroyRoll == 3 && true)//Should include a NULL check for armor module and an active check instead of true
            {
                //Deactivate armor module
            }
            else
            {
                //Allow for choice by attacker of destroyed module out of the current ones active
            }
        }
    }

    public int GetActiveModuleAmount()
    {
        int amount = 0;

        if (true)//Should be a check for if the current weapon is active
            amount++;

        if (true)//Should be a check for if the current engine is active
            amount++;

        if (true)//Should be a check for if the current armor is active
            amount++;

        return amount;
    }

    public int GetWeaponDamage()
    {
        int damage;

        if (true)//Should be a NULL check for the weapon module once reference is created
        {
            damage = Random.Range(1, 7);//*REPLACE WITH* Dice roll of default weapon dice
        }
        else
        {
            damage = Random.Range(1, 7);//*REPLACE WITH* Dice roll of current weapon dice
            if (true)//Should be a check for if the current weapon is active
                damage += 1;//*REPLACE WITH* Current weapon modifier

        }

        return damage;
    }

    public int GetMoveDistance(bool addBonus)
    {
        int distance;

        if (true)//Should be a NULL check for the weapon module once reference is created
        {
            distance = Random.Range(1, 7);//*REPLACE WITH* Dice roll of default engine dice
        }
        else
        {
            distance = Random.Range(1, 7);//*REPLACE WITH* Dice roll of the current
            if (addBonus && true)//Should replace 'true' with check for if current weapon is active
                distance += 1;//*REPLACE WITH* Current engine modifier
        }

        return distance;
    }

    public int GetArmor()
    {
        int armor = 0;

        if (true && true)//Should be a NULL check for the weapon module once reference is created AND a check for if current armor module is active
        {
            armor = 1;//*REPLACE WITH* Armor modifier from armor module
        }

        return armor;
    }
}
