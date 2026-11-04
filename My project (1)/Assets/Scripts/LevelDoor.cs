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
                DataManager.EnterLevel(level);
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
        if (level == 1 || DataManager.CompletedLevel(level - 1))
        {
            return true;
        }

        return false;
    }
}
