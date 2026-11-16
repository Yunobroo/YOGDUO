using UnityEngine;

public class HideSpot : MonoBehaviour
{
    public float radius = 1.25f;

    void Start()
    {
        // Beds store their hide zone on an empty child; outline the actual bed.
        GameObject visual = gameObject;
        if (GetComponentInChildren<Renderer>() == null && transform.parent != null &&
            transform.parent.GetComponent<Renderer>() != null) visual = transform.parent.gameObject;
        WorldHighlight.Add(visual, new Color(.2f, 1f, .55f));
    }

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
