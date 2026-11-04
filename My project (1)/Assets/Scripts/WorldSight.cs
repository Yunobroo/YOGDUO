using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldSight
{
    static readonly RaycastHit[] hits = new RaycastHit[128];
    // Imported pillars and props often use Default rather than the old wall layer.
    // Rendering transparency must never change whether a guard can see through them.
    public static bool Blocked(Vector3 from, Vector3 to, Transform observer = null, Transform subject = null)
    {
        Vector3 direction = to - from;
        if (direction.sqrMagnitude < .000001f) return false;
        var physics = observer != null ? observer.gameObject.scene.GetPhysicsScene() : Physics.defaultPhysicsScene;
        int count = physics.Raycast(from, direction.normalized, hits, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            Transform obstacle = hit.collider.transform;
            if (observer != null && obstacle.IsChildOf(observer) || subject != null && obstacle.IsChildOf(subject)) continue;
            if (hit.collider.GetComponentInParent<Player>() != null ||
                hit.collider.GetComponentInParent<SearchAndChase>() != null ||
                hit.collider.GetComponentInParent<Collectible>() != null) continue;
            return true;
        }
        return false;
    }
}
