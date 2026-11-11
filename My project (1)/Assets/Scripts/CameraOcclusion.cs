using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// See-through walls, with every design we tried kept so the design process can be shown.
// Press F5 in Play mode (or change "design" on the camera's CameraOcclusion) to cycle:
//   1 Original fade - walls in the way turn 30% transparent (first version)
//   2 Peephole      - walls stay solid; a clean round hole with a soft glowing rim opens
//                     around the goblin (default)
//   3 Silhouette    - walls never change; the goblin and guards glow through them
//   4 Stone fade    - the original fade, washed dusty grey-brown
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Camera))]
public class CameraOcclusion : MonoBehaviour
{
    public enum Design { OriginalFade, Peephole, Silhouette, StoneFade }

    [Tooltip("F5 cycles through the designs while playing.")]
    public Design design = Design.Peephole;
    public Transform target;

    [Header("Fade designs (1 and 4)")]
    public float fadedOpacity = .3f;
    [Tooltip("Stone fade: faded walls are washed toward this colour.")]
    public Color fadeTint = new Color(.62f, .53f, .43f);
    [Range(0, 1)] public float tintStrength = .85f;

    [Header("Peephole design (2)")]
    [Tooltip("Hole radius as a fraction of screen height.")]
    [Range(.03f, .5f)] public float holeRadius = .08f;
    [Tooltip("How far the rim glow spreads outward, as a fraction of the hole radius.")]
    [Range(0, 1)] public float edgeSoftness = .35f;
    [Tooltip("Colour of the soft glow around the hole.")]
    public Color rimColor = new Color(1f, .6f, .25f);
    [Tooltip("Brightness of the rim glow (0 = no glow, just a clean edge).")]
    [Range(0, 2)] public float rimStrength = 0f;
    [Tooltip("Height (m) above the goblin's feet that the hole never cuts, so the empty space under walls stays hidden.")]
    [Range(0, 1.5f)] public float holeSill = .45f;

    [Header("Silhouette design (3)")]
    public Color goblinGlow = new Color(.45f, 1f, .4f, .6f);
    public Color guardGlow = new Color(1f, .25f, .15f, .5f);

    [Header("Timing")]
    public float fadeSpeed = 3.5f;
    public float occlusionDelay = .18f;
    public float releaseDelay = .1f;

    static readonly string[] DesignNames = { "Original fade", "Peephole (final)", "Silhouette", "Stone fade" };
    static readonly string[] DesignNotes =
    {
        "Walls in the way turn 30% see-through",
        "Walls stay solid - a hole opens around the goblin",
        "Walls never change - goblin and guards glow through them",
        "The original fade, washed dusty grey-brown"
    };

    class Wall
    {
        public Renderer renderer;
        public Material[] original, swapped;
        public float amount;   // 0 = untouched, 1 = fully faded / hole fully open
        public float lastBlocked;
    }
    readonly Dictionary<Renderer, Wall> walls = new Dictionary<Renderer, Wall>();
    readonly HashSet<Renderer> blocking = new HashSet<Renderer>();
    readonly List<Renderer> restored = new List<Renderer>();
    readonly Dictionary<Renderer, float> pending = new Dictionary<Renderer, float>();
    Shader fadeShader, peepholeLit, peepholeSimple;
    Camera view;
    bool hasActiveDesign;
    Design activeDesign;
    float labelUntil = -10;
    readonly RaycastHit[] hits = new RaycastHit[128];

    static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    static readonly int TintId = Shader.PropertyToID("_Tint");
    static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
    static readonly int PeepOpenId = Shader.PropertyToID("_PeepOpen");
    static readonly int PeepTargetId = Shader.PropertyToID("_PeepholeTarget");
    static readonly int PeepShapeId = Shader.PropertyToID("_PeepholeShape");
    static readonly int PeepRimId = Shader.PropertyToID("_PeepholeRimColor");

    bool IsPeephole => design == Design.Peephole;

    void Awake()
    {
        fadeShader = Resources.Load<Shader>("Visibility/OccluderFade");
        peepholeLit = Resources.Load<Shader>("Visibility/PeepholeLit");
        peepholeSimple = Resources.Load<Shader>("Visibility/PeepholeSimple");
        view = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        var keys = Keyboard.current;
        if (keys != null && keys.f5Key.wasPressedThisFrame)
        {
            design = (Design)(((int)design + 1) % DesignNames.Length);
            labelUntil = Time.unscaledTime + 3f;
        }
        if (!hasActiveDesign || design != activeDesign) SwitchDesign();

        blocking.Clear();
        if (target != null && fadeShader != null && design != Design.Silhouette)
        {
            Vector3 destination = target.position + Vector3.up * .65f;
            if (IsPeephole)
            {
                Shader.SetGlobalVector(PeepTargetId, destination);
                Shader.SetGlobalVector(PeepShapeId, new Vector4(holeRadius, edgeSoftness, rimStrength, holeSill));
                Shader.SetGlobalColor(PeepRimId, rimColor);
            }
            FindBlockers(destination);
        }
        restored.Clear();
        foreach (var candidate in pending.Keys) if (!blocking.Contains(candidate)) restored.Add(candidate);
        foreach (var candidate in restored) pending.Remove(candidate);
        restored.Clear();
        foreach (var pair in walls)
        {
            Wall wall = pair.Value;
            bool blocked = blocking.Contains(pair.Key) || Time.time - wall.lastBlocked < releaseDelay;
            wall.amount = Mathf.MoveTowards(wall.amount, blocked ? 1 : 0, Time.deltaTime * fadeSpeed);
            Apply(wall);
            if (wall.renderer == null || wall.amount <= 0 && !blocked) restored.Add(pair.Key);
        }
        foreach (var renderer in restored)
        {
            Restore(walls[renderer]);
            walls.Remove(renderer);
            pending.Remove(renderer);
        }
    }

    void SwitchDesign()
    {
        foreach (var wall in walls.Values) Restore(wall);
        walls.Clear();
        pending.Clear();
        activeDesign = design;
        hasActiveDesign = true;
        SetSilhouettes(design == Design.Silhouette);
    }

    void SetSilhouettes(bool on)
    {
        var game = GoblinDungeon.Instance;
        if (on && game != null)
        {
            if (game.player != null) XRaySilhouette.Add(game.player.gameObject, goblinGlow);
            if (game.guards != null)
                foreach (var guard in game.guards) if (guard != null) XRaySilhouette.Add(guard.gameObject, guardGlow);
        }
        foreach (var silhouette in FindObjectsByType<XRaySilhouette>()) silhouette.enabled = on;
    }

    void FindBlockers(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        float distance = direction.magnitude;
        if (distance < .5f) return;
        var ray = new Ray(transform.position, direction / distance);
        var physics = gameObject.scene.GetPhysicsScene();
        int hitCount;
        if (IsPeephole)
        {
            // The hole's radius in world units at the goblin's distance, so every wall piece it overlaps is found.
            float worldRadius = view != null && view.orthographic
                ? holeRadius * 2f * view.orthographicSize
                : holeRadius * 2f * distance * Mathf.Tan((view != null ? view.fieldOfView : 60f) * .5f * Mathf.Deg2Rad);
            worldRadius = Mathf.Min(worldRadius, distance * .5f);
            hitCount = physics.SphereCast(ray.origin, worldRadius, ray.direction, hits, Mathf.Max(0, distance - .4f), ~0, QueryTriggerInteraction.Ignore);
        }
        else hitCount = physics.Raycast(ray.origin, ray.direction, hits, Mathf.Max(0, distance - .4f), ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            var collider = hits[i].collider;
            if (collider.bounds.size.y < 1f || collider.GetComponentInParent<Player>() != null ||
                collider.GetComponentInParent<SearchAndChase>() != null || collider.GetComponentInParent<HideSpot>() != null ||
                collider.GetComponentInChildren<HideSpot>() != null ||
                collider.GetComponentInParent<Collectible>() != null) continue;
            // The peephole never cuts floors or anything that doesn't rise above the goblin.
            if (IsPeephole && collider.bounds.max.y < target.position.y + .5f) continue;
            var renderers = collider.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                var parent = collider.GetComponentInParent<Renderer>();
                if (parent != null) renderers = new[] { parent };
            }
            foreach (var renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer.GetComponentInParent<WorldHighlight>() != null) continue;
                // Fades only touch renderers the sight ray really passes through.
                if (!IsPeephole && (!renderer.bounds.IntersectRay(ray, out float hitDistance) || hitDistance >= distance - .4f)) continue;
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

    void Track(Renderer renderer)
    {
        Material[] original = renderer.sharedMaterials;
        if (original.Length == 0) return;
        foreach (var material in original) if (material == null || material.renderQueue >= 3000) return;
        var wall = new Wall { renderer = renderer, original = original, swapped = new Material[original.Length], lastBlocked = Time.time };
        for (int i = 0; i < original.Length; i++) wall.swapped[i] = MakeCopy(original[i]);
        Apply(wall);
        renderer.sharedMaterials = wall.swapped;
        walls.Add(renderer, wall);
    }

    Material MakeCopy(Material source)
    {
        if (IsPeephole && peepholeLit != null && source.shader != null && source.shader.name == "Universal Render Pipeline/Lit")
            return new Material(source) { shader = peepholeLit }; // identical look apart from the hole

        var copy = new Material(IsPeephole && peepholeSimple != null ? peepholeSimple : fadeShader);
        if (source.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        else if (source.HasProperty("_Color")) copy.SetColor("_BaseColor", source.GetColor("_Color"));
        string texture = source.HasProperty("_BaseMap") ? "_BaseMap" : source.HasProperty("_MainTex") ? "_MainTex" : null;
        if (texture != null)
        {
            copy.SetTexture("_BaseMap", source.GetTexture(texture));
            copy.SetTextureScale("_BaseMap", source.GetTextureScale(texture));
            copy.SetTextureOffset("_BaseMap", source.GetTextureOffset(texture));
        }
        return copy;
    }

    void Apply(Wall wall)
    {
        foreach (var material in wall.swapped)
        {
            if (material == null) continue;
            if (IsPeephole) { material.SetFloat(PeepOpenId, wall.amount); continue; }
            material.SetFloat(OpacityId, Mathf.Lerp(1, fadedOpacity, wall.amount));
            material.SetColor(TintId, fadeTint);
            // The stone tint grows with the fade, so the wall eases into grey-brown rather than snapping.
            material.SetFloat(TintStrengthId, design == Design.StoneFade ? tintStrength * wall.amount : 0);
        }
    }

    static void Restore(Wall wall)
    {
        if (wall.renderer != null) wall.renderer.sharedMaterials = wall.original;
        foreach (var material in wall.swapped)
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
        hasActiveDesign = false;
    }

    // Brief banner naming the design after each F5 press.
    void OnGUI()
    {
        if (!Application.isPlaying || Time.unscaledTime > labelUntil) return;
        var skin = DungeonUI.ForScreen();
        float ui = skin.ui;
        float alpha = Mathf.Clamp01((labelUntil - Time.unscaledTime) * 2);
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        float width = Mathf.Min(440 * ui, Screen.width - 32);
        var r = new Rect((Screen.width - width) * .5f, Screen.height * .22f, width, 62 * ui);
        DungeonUI.Plaque(r, ui, .95f * alpha, DungeonUI.Torch);
        int index = (int)design;
        DungeonUI.Text(new Rect(r.x, r.y + 8 * ui, r.width, 24 * ui),
            $"SEE-THROUGH DESIGN {index + 1}/{DesignNames.Length}: {DesignNames[index]}", skin.bodyCenter, DungeonUI.Torch, alpha);
        DungeonUI.Text(new Rect(r.x, r.y + 34 * ui, r.width, 20 * ui),
            DesignNotes[index] + "   (F5 next)", skin.bodyCenter, DungeonUI.Parchment, alpha);
        GUI.matrix = oldMatrix;
    }
}
