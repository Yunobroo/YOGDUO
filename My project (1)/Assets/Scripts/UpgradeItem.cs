using UnityEngine;


public abstract class UpgradeItem : ScriptableObject
{
    public new string name;
    public string buyText;
    public int cost;
    public int maxLevel = 1;
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
