using UnityEngine;

[CreateAssetMenu(fileName = "SprintWhileCarrying", menuName = "ScriptableObjects/Sprint While Carrying", order = 4)]
public class SprintWhileCarrying : UpgradeItem
{
    public override void OnBuy()
    {
        DataManager.Data.sprintWhileCarrying = true;
    }

    public override int Owned()
    {
        return DataManager.Data.sprintWhileCarrying ? 1 : 0;
    }
}
