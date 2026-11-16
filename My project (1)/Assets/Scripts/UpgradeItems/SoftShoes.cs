using UnityEngine;

[CreateAssetMenu(fileName = "SoftShoes", menuName = "ScriptableObjects/SoftShoes", order = 2)]
public class SoftShoes : UpgradeItem
{
    public override void OnBuy()
    {
        DataManager.Data.softShoes = true;
    }

    public override int Owned()
    {
        return DataManager.Data.softShoes ? 1 : 0;
    }
}
