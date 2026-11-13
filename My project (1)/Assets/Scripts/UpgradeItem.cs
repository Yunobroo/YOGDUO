using UnityEngine;


public abstract class UpgradeItem : ScriptableObject
{
    public new string name;
    public string buyText;
    [Tooltip("Price of the first level.")]
    public int cost;
    [Tooltip("Added to the price for each level already owned (0 = same price every level).")]
    public int costIncrease = 0;
    public int maxLevel = 1;

    // Price of the next level: cost, then cost + costIncrease, cost + 2 * costIncrease, ...
    public int CurrentCost() => cost + costIncrease * Owned();
    [Tooltip("0 to disable")]
    public int orbNeeded = 0;

    //Use override to provide an effect. Here is universal error message
    public void Effect()
    {
        Debug.LogWarning($"Some script is trying to call {this}.Effect() yet it has none!");
    }

    public abstract void OnBuy();

    public abstract int Owned();
}
