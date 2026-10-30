using UnityEngine;
using UnityEngine.AI;

public class Collectible : MonoBehaviour
{
    public enum Kind { Coin, Gem, Orb, Crown, Key }
    public Kind kind;
    public Transform visual;
    public float pickupRadius = .8f;
    public LayerMask wallMask = 1 << 8;
    bool collected;
    public bool PlayerCarrying { get; private set; }
    public bool Dropped { get; private set; }
    public SearchAndChase GuardCarrier { get; private set; }
    public Vector3 HomePosition { get; private set; }
    Transform homeParent;
    Quaternion homeRotation;
    Vector3 homeScale, visualPosition;
    float playerPickupAllowedAt;

    void Awake()
    {
        HomePosition = transform.position;
        homeParent = transform.parent;
        homeRotation = transform.rotation;
        homeScale = transform.localScale;
        if (visual != null) visualPosition = visual.localPosition;
    }

    void Update()
    {
        if (collected || PlayerCarrying || GuardCarrier != null) return;
        if (visual != null) visual.Rotate(0, 65 * Time.deltaTime, 0, Space.World);
        var game = GoblinDungeon.Instance;
        if (game == null || !game.Playing || game.player == null) return;
        if (Time.time < playerPickupAllowedAt) return;
        Vector3 playerPosition = game.player.transform.position;
        if (Vector3.Distance(playerPosition, transform.position) > pickupRadius) return;
        if (Physics.Linecast(playerPosition + Vector3.up * .5f, transform.position + Vector3.up * .5f, wallMask)) return;
        if (kind == Kind.Crown || kind == Kind.Key)
        {
            if (game.CarryingCrown || game.CarryingKey) return; 
            PlayerCarrying = true;
            Dropped = false;
            AttachTo(game.player.transform);
        }
        else
        {
            game.Collect(kind);
            collected = true;
            gameObject.SetActive(false);
        }

    }

    void AttachTo(Transform carrier)
    {
        transform.SetParent(carrier, false);
        transform.localPosition = new Vector3(0, .8f, .55f); 
        transform.eulerAngles = new Vector3(0, 90, 0);
        if (visual != null)
        {
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
        }
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
    }

    public void Drop(Vector3 position)
    {
        if (!PlayerCarrying) return;
        PlayerCarrying = false;
        Dropped = true;
        playerPickupAllowedAt = Time.time + 1.5f;
        // Put the dropped crown on a reachable floor rather than inside a wall.
        if (NavMesh.SamplePosition(position, out var hit, 1.5f, NavMesh.AllAreas)) position = hit.position;
        RestoreWorldPose(position);
    }

    public bool TryGuardPickup(SearchAndChase guard)
    {
        if (!Dropped ||                 //If not dropped
            GuardCarrier != null ||     //If already targeted
            guard == null ||            //If input was null
            kind == Kind.Key)           //Don't pick up keys
            return false;
        if (Vector3.Distance(guard.transform.position, transform.position) > pickupRadius) return false;
        if (Physics.Linecast(guard.transform.position + Vector3.up * .5f,
            transform.position + Vector3.up * .5f, wallMask)) return false;
        Dropped = false;
        GuardCarrier = guard;
        AttachTo(guard.transform);
        return true;
    }

    public void ReturnHome(SearchAndChase guard)
    {
        if (GuardCarrier != guard) return;
        GuardCarrier = null;
        RestoreWorldPose(HomePosition);
        playerPickupAllowedAt = Time.time + .5f;
    }

    void RestoreWorldPose(Vector3 position)
    {
        transform.SetParent(homeParent, true);
        transform.position = position;
        transform.rotation = homeRotation;
        transform.localScale = homeScale;
        if (visual != null) visual.localPosition = visualPosition;
    }
}
