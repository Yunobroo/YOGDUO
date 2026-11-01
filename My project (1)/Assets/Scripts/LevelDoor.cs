using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelDoor : MonoBehaviour
{
    [SerializeField]
    private int level;
    public float interactionRange;

    public bool InRange()
    {
        var playerPos = FindAnyObjectByType<Player>().transform.position;
        Vector3 offset = playerPos - transform.position;
        offset.y = 0;
        return isActiveAndEnabled && offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && InRange())
        {
            Debug.Log($"Interacted to go to Level {level}. Unlocked: {Unlocked()}");
            if (Unlocked())
            {
                Debug.Log("Entering Level...");
                HeistProgress.EnterLevel(level);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    private bool Unlocked()
    {
        if (level == 1 || HeistProgress.data.completedLevels.Contains(level - 1))//If one of the completed levels is the previous level or if it is the first level
        {
            return true;
        }

        return false;
    }
}
