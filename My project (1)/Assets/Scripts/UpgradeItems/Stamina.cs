using UnityEngine;
[CreateAssetMenu(fileName = "Stamina", menuName = "ScriptableObjects/Stamina", order = 3)]
public class Stamina : UpgradeItem
{
    public override void OnBuy()
    {
        DataManager.Data.stamina++;
    }

    public override int Owned()
    {
        return DataManager.Data.stamina;
    }
}
