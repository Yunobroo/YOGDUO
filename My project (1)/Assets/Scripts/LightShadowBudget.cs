using UnityEngine;

// Performance: every torch is a point light with soft shadows, and a shadowed point light
// renders the scene six times (one per cube face) for its shadow map. With 40+ torches per
// level that dominated the frame. Only the torches nearest the goblin keep their shadows;
// all of them still light the scene. Added by GoblinDungeon at startup.
public class LightShadowBudget : MonoBehaviour
{
    [Tooltip("How many point/spot lights nearest the player may cast shadows.")]
    [Min(0)] public int shadowedLights = 4;
    [Tooltip("Seconds between re-picking the nearest lights.")]
    [Min(.05f)] public float interval = .25f;

    Light[] lights;
    LightShadows[] authored;   // each light's own shadow setting, restored when it is near
    float[] distances;
    int[] order;
    float next;

    void Start()
    {
        var all = FindObjectsByType<Light>();
        int count = 0;
        foreach (var light in all) if (light.type == LightType.Point || light.type == LightType.Spot) count++;
        lights = new Light[count];
        authored = new LightShadows[count];
        distances = new float[count];
        order = new int[count];
        int i = 0;
        foreach (var light in all)
        {
            if (light.type != LightType.Point && light.type != LightType.Spot) continue;
            lights[i] = light;
            authored[i] = light.shadows;
            i++;
        }
    }

    void Update()
    {
        if (lights == null || Time.time < next) return;
        next = Time.time + interval;
        var game = GoblinDungeon.Instance;
        if (game == null || game.player == null) return;
        Vector3 player = game.player.transform.position;

        for (int i = 0; i < lights.Length; i++)
        {
            order[i] = i;
            distances[i] = lights[i] != null ? (lights[i].transform.position - player).sqrMagnitude : float.MaxValue;
        }
        System.Array.Sort(distances, order);
        for (int rank = 0; rank < order.Length; rank++)
        {
            int i = order[rank];
            if (lights[i] == null) continue;
            var wanted = rank < shadowedLights ? authored[i] : LightShadows.None;
            if (lights[i].shadows != wanted) lights[i].shadows = wanted;
        }
    }

    void OnDisable()
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].shadows = authored[i];
    }
}
