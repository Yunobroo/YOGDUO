using UnityEngine;

public class HideSpot : MonoBehaviour
{
    public float radius = 1.25f;

    public bool Contains(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0;
        return isActiveAndEnabled && offset.sqrMagnitude <= radius * radius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
