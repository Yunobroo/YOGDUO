using UnityEngine;
[CreateAssetMenu(fileName = "Boots", menuName = "ScriptableObjects/Boots", order = 1)]
public class Boots : UpgradeItem
{
    public override void OnBuy()
    {
        DataManager.Data.boots++;
    }

    public override int Owned()
    {
        return DataManager.Data.boots;
    }
}
