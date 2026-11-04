using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Camera))]
public class CameraOcclusion : MonoBehaviour
{
    public Transform target;
    public float fadedOpacity = .3f;
    public float fadeSpeed = 3.5f;
    public float occlusionDelay = .18f;
    public float releaseDelay = .1f;
    class Wall
    {
        public Renderer renderer;
        public Material[] original, fading;
        public float opacity = 1;
        public float lastBlocked;
    }
    readonly Dictionary<Renderer, Wall> walls = new Dictionary<Renderer, Wall>();
    readonly HashSet<Renderer> blocking = new HashSet<Renderer>();
    readonly List<Renderer> restored = new List<Renderer>();
    readonly Dictionary<Renderer, float> pending = new Dictionary<Renderer, float>();
    Shader shader;
    readonly RaycastHit[] hits = new RaycastHit[128];

    void Awake() => shader = Resources.Load<Shader>("Visibility/OccluderFade");

    void LateUpdate()
    {
        blocking.Clear();
        if (target != null && shader != null)
        {
            Vector3 destination = target.position + Vector3.up * .65f;
            Vector3 direction = destination - transform.position;
            var ray = new Ray(transform.position, direction.normalized);
            int hitCount = gameObject.scene.GetPhysicsScene().Raycast(ray.origin, ray.direction, hits,
                Mathf.Max(0, direction.magnitude - .4f), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = hits[i].collider;
                if (collider.bounds.size.y < 1f || collider.GetComponentInParent<Player>() != null ||
                    collider.GetComponentInParent<SearchAndChase>() != null || collider.GetComponentInParent<HideSpot>() != null ||
                    collider.GetComponentInChildren<HideSpot>() != null ||
                    collider.GetComponentInParent<Collectible>() != null) continue;
                var renderers = collider.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    var parent = collider.GetComponentInParent<Renderer>();
                    if (parent != null) renderers = new[] { parent };
                }
                foreach (var renderer in renderers)
                {
                    if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer.GetComponentInParent<WorldHighlight>() != null) continue;
                    if (!renderer.bounds.IntersectRay(ray, out float distance) || distance >= direction.magnitude - .4f) continue;
                    blocking.Add(renderer);
                    if (walls.TryGetValue(renderer, out Wall existing)) existing.lastBlocked = Time.time;
                    else
                    {
                        if (!pending.ContainsKey(renderer)) pending.Add(renderer, Time.time);
                        if (Time.time - pending[renderer] >= occlusionDelay) Track(renderer);
                    }
                }
            }
        }
        restored.Clear();
        foreach (var candidate in pending.Keys) if (!blocking.Contains(candidate)) restored.Add(candidate);
        foreach (var candidate in restored) pending.Remove(candidate);
        restored.Clear();
        foreach (var pair in walls)
        {
            Wall wall = pair.Value;
            bool blocked = blocking.Contains(pair.Key) || Time.time - wall.lastBlocked < releaseDelay;
            wall.opacity = Mathf.MoveTowards(wall.opacity, blocked ? fadedOpacity : 1, Time.deltaTime * fadeSpeed);
            foreach (var material in wall.fading) if (material != null) material.SetFloat("_Opacity", wall.opacity);
            if (wall.renderer == null || wall.opacity >= 1 && !blocked) restored.Add(pair.Key);
        }
        foreach (var renderer in restored)
        {
            Restore(walls[renderer]);
            walls.Remove(renderer);
            pending.Remove(renderer);
        }
    }

    void Track(Renderer renderer)
    {
        Material[] original = renderer.sharedMaterials;
        if (original.Length == 0) return;
        foreach (var material in original) if (material == null || material.renderQueue >= 3000) return;
        var wall = new Wall { renderer = renderer, original = original, fading = new Material[original.Length], lastBlocked = Time.time };
        for (int i = 0; i < original.Length; i++)
        {
            var source = original[i];
            var fade = new Material(shader);
            if (source.HasProperty("_BaseColor")) fade.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            else if (source.HasProperty("_Color")) fade.SetColor("_BaseColor", source.GetColor("_Color"));
            string texture = source.HasProperty("_BaseMap") ? "_BaseMap" : source.HasProperty("_MainTex") ? "_MainTex" : null;
            if (texture != null)
            {
                fade.SetTexture("_BaseMap", source.GetTexture(texture));
                fade.SetTextureScale("_BaseMap", source.GetTextureScale(texture));
                fade.SetTextureOffset("_BaseMap", source.GetTextureOffset(texture));
            }
            fade.SetFloat("_Opacity", 1);
            wall.fading[i] = fade;
        }
        renderer.sharedMaterials = wall.fading;
        walls.Add(renderer, wall);
    }

    static void Restore(Wall wall)
    {
        if (wall.renderer != null) wall.renderer.sharedMaterials = wall.original;
        foreach (var material in wall.fading)
            if (material != null)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
    }

    void OnDisable()
    {
        foreach (var wall in walls.Values) Restore(wall);
        walls.Clear();
        pending.Clear();
    }
}
