using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldHighlight : MonoBehaviour
{
    readonly List<GameObject> outlines = new List<GameObject>();
    Material material;

    public static void Add(GameObject owner, Color color)
    {
        if (owner.GetComponent<WorldHighlight>() != null) return;
        owner.AddComponent<WorldHighlight>().Build(color);
    }

    void Build(Color color)
    {
        var shader = Resources.Load<Shader>("Visibility/Outline");
        if (shader == null) return;
        material = new Material(shader);
        material.SetColor("_Color", color);
        foreach (var source in GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;
            var skin = source as SkinnedMeshRenderer;
            var filter = source.GetComponent<MeshFilter>();
            if (skin != null) mesh = skin.sharedMesh;
            else if (source is MeshRenderer && filter != null) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            var outline = new GameObject("Highlight");
            outline.layer = source.gameObject.layer;
            outline.transform.SetParent(source.transform, false);
            Renderer renderer;
            if (skin != null)
            {
                var copy = outline.AddComponent<SkinnedMeshRenderer>();
                copy.sharedMesh = mesh;
                copy.bones = skin.bones;
                copy.rootBone = skin.rootBone;
                copy.localBounds = skin.localBounds;
                renderer = copy;
            }
            else
            {
                outline.AddComponent<MeshFilter>().sharedMesh = mesh;
                renderer = outline.AddComponent<MeshRenderer>();
            }
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.enabled = source.enabled;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            outlines.Add(outline);
        }
    }

    void OnEnable() { foreach (var outline in outlines) if (outline != null) outline.SetActive(true); }
    void OnDisable() { foreach (var outline in outlines) if (outline != null) outline.SetActive(false); }
    void OnDestroy()
    {
        foreach (var outline in outlines) if (outline != null) Destroy(outline);
        if (material != null) Destroy(material);
    }
}
