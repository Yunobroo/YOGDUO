using UnityEngine;

// Follow the character without moving the parent that also contains the character.
[RequireComponent(typeof(Camera))]
public class PlayerCameraFollow : MonoBehaviour
{
    public Transform target;
    Vector3 offset;
    float height;

    void Start()
    {
        if (target == null && GoblinDungeon.Instance != null && GoblinDungeon.Instance.player != null)
            target = GoblinDungeon.Instance.player.transform;
        if (target == null) return;
        var occlusion = GetComponent<CameraOcclusion>();
        if (occlusion == null) occlusion = gameObject.AddComponent<CameraOcclusion>();
        occlusion.target = target;
        offset = transform.position - target.position;
        height = transform.position.y;
    }

    void LateUpdate()
    {
        if (target == null) return;
        // Keep height steady: floor contact corrections should not shake the camera.
        transform.position = new Vector3(target.position.x + offset.x, height, target.position.z + offset.z);
    }
}
