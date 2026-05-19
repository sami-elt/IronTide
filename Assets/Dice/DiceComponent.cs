using TMPro;
using UnityEngine;

public class DiceComponent : MonoBehaviour
{

    public TMP_Text resultText;
    public int RollDice(int sides)
    {
        int result = Random.Range(1, sides + 1);

        Debug.Log("tärning" + sides +"resultat" + result);

        if (resultText != null)
        {
            resultText.text = result.ToString();
        }

        DiceVisualManager.ShowRoll(sides, result, transform);

        return result;
    }

    public int RollD4()
    {
        return RollDice(4);
    }
    public int RollD6()
    {
        return RollDice(6);
    }
    public int RollD8()
    {
        return RollDice(8);
    }

    public void Test_RollD6()
    {
        RollD6();
    }
}
