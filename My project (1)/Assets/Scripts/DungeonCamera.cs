using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class DungeonCamera : MonoBehaviour
{
    public Transform target;
    [Min(1f)] public float zoom = 8f;
    [Min(0f)] public float followSmoothTime = .12f;
    public Vector3 offset = new Vector3(20, 25, -20);
    Camera view;
    Vector3 velocity;

    void OnEnable()
    {
        view = GetComponent<Camera>();
        velocity = Vector3.zero;
        UpdateCamera(true);
    }

    void LateUpdate() => UpdateCamera(!Application.isPlaying);

    void UpdateCamera(bool snap)
    {
        if (view == null) return;
        if (target == null && Application.isPlaying && GoblinDungeon.Instance != null && GoblinDungeon.Instance.player != null)
            target = GoblinDungeon.Instance.player.transform;
        view.orthographic = true;
        // Close gameplay framing; retain enough horizontal view in narrow windows.
        view.orthographicSize = zoom * Mathf.Max(1f, (4f / 3f) / Mathf.Max(.1f, view.aspect));
        if (target == null) return;
        Vector3 destination = target.position + offset;
        transform.position = snap || followSmoothTime <= 0
            ? destination
            : Vector3.SmoothDamp(transform.position, destination, ref velocity, followSmoothTime);
    }
}
