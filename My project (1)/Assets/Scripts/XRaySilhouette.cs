using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Silhouette design: gives a character a glowing shape that shows only while walls or props hide it.
// Copies each mesh (skinned meshes keep their bones) with Goblin/XRay. CameraOcclusion
// adds and enables these only while the Silhouette design is selected.
public class XRaySilhouette : MonoBehaviour
{
    const string CopyName = "XRay";
    readonly List<GameObject> copies = new List<GameObject>();
    Material material;

    public static XRaySilhouette Add(GameObject owner, Color color)
    {
        if (owner == null) return null;
        var existing = owner.GetComponent<XRaySilhouette>();
        if (existing != null) return existing;
        var silhouette = owner.AddComponent<XRaySilhouette>();
        silhouette.Build(color);
        return silhouette;
    }

    void Build(Color color)
    {
        var shader = Resources.Load<Shader>("Visibility/XRay");
        if (shader == null) return;
        material = new Material(shader);
        material.SetColor("_Color", color);
        foreach (var source in GetComponentsInChildren<Renderer>(true))
        {
            // Skip other overlays (WorldHighlight outlines, earlier copies) and effects.
            if (source.name == CopyName || source.name == "Highlight" ||
                source is ParticleSystemRenderer || source is LineRenderer) continue;
            Mesh mesh = null;
            var skin = source as SkinnedMeshRenderer;
            var filter = source.GetComponent<MeshFilter>();
            if (skin != null) mesh = skin.sharedMesh;
            else if (source is MeshRenderer && filter != null) mesh = filter.sharedMesh;
            if (mesh == null) continue;

            var copy = new GameObject(CopyName);
            copy.layer = source.gameObject.layer;
            copy.transform.SetParent(source.transform, false);
            Renderer renderer;
            if (skin != null)
            {
                var skinned = copy.AddComponent<SkinnedMeshRenderer>();
                skinned.sharedMesh = mesh;
                skinned.bones = skin.bones;
                skinned.rootBone = skin.rootBone;
                skinned.localBounds = skin.localBounds;
                renderer = skinned;
            }
            else
            {
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                renderer = copy.AddComponent<MeshRenderer>();
            }
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.enabled = source.enabled;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            copy.SetActive(enabled);
            copies.Add(copy);
        }
    }

    void OnEnable() { foreach (var copy in copies) if (copy != null) copy.SetActive(true); }
    void OnDisable() { foreach (var copy in copies) if (copy != null) copy.SetActive(false); }
    void OnDestroy()
    {
        foreach (var copy in copies) if (copy != null) Destroy(copy);
        if (material != null) Destroy(material);
    }
}
