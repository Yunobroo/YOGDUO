using UnityEngine;
using UnityEngine.AI;

// Each dungeon owns its navigation asset, including when scenes are changed.
[ExecuteAlways]
[DefaultExecutionOrder(-1000)]
public class BakedNavigation : MonoBehaviour
{
    public NavMeshData data;
    NavMeshDataInstance instance;

    void OnEnable()
    {
        if (data != null) instance = NavMesh.AddNavMeshData(data);
    }

    void OnDisable()
    {
        if (instance.valid) instance.Remove();
    }
}
