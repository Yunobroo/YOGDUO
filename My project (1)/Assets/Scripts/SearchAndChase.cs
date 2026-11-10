using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Adapted from the supplied SearchAndChase: patrol, chase, then last known location.
public class SearchAndChase : MonoBehaviour
{
    [Header("FOV Size")]
    [Range(0, 360)] public float angle = 85;
    public float radius = 7;
    [Tooltip("Noticed at this range regardless of facing (60% of it when sneaking).")]
    public float closeRange = 1.5f;
    [Tooltip("Seconds a chasing guard keeps tracking the player after losing sight.")]
    public float chaseMemory = 1.2f;
    [Header("Masks")]
    public GameObject target;
    public string targetTag = "Player";
    public LayerMask targetMask;
    public LayerMask visionBlockMask = 1 << 8;
    public NavMeshAgent agent;
    public bool CanSeeTarget { get; private set; }
    [Header("Destinations")]
    public float destinationTolerance = .5f;
    public string patrolPointTag;
    public Transform[] patrolPoints;
    public string Status => state.ToString();
    public bool Investigating => state == State.LastKnownLocation || state == State.Lingering;
    enum State { Patrolling, Chasing, LastKnownLocation, Lingering, RecoveringCrown, ReturningCrown }
    State state;
    Vector3 currentDestination;
    Vector3 requestedDestination;
    bool hasRequestedDestination;
    int patrolIndex;
    float searchTime;
    float lastSeenTime = float.NegativeInfinity;
    Player player;
    int heardFootstep;
    Coroutine lingerRoutine;

    void Start()
    {
        WorldHighlight.Add(gameObject, new Color(1f, .2f, .12f));
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        EnsureNavigation();
        if (target != null) player = target.GetComponent<Player>();
        // The rig and character can both be tagged Player. Chase the controller,
        // not its stationary parent or camera rig.
        if (player == null)
            player = GoblinDungeon.Instance != null ? GoblinDungeon.Instance.player : FindAnyObjectByType<Player>();
        if (player != null) target = player.gameObject;
        SelectPatrolPoint();
        StartCoroutine(FOVRoutine());
    }

    void Update()
    {
        if (!EnsureNavigation()) return;
        if (GoblinDungeon.Instance == null || !GoblinDungeon.Instance.Playing) { agent.isStopped = true; return; }
        agent.isStopped = false;
        if (RecoverCrown()) return;
        // Lose sight immediately when the player moves behind solid cover.
        if (CanSeeTarget) FieldOfViewCheck();
        if (CanSeeTarget) lastSeenTime = Time.time;
        // Brief sight breaks (a pillar, a sharp turn) don't end a chase; hiding does.
        bool stillOnTrail = state == State.Chasing && player != null && !player.Hidden &&
                            Time.time - lastSeenTime < chaseMemory;
        if (CanSeeTarget || stillOnTrail)
        {
            StopLingering();
            state = State.Chasing;
            currentDestination = target.transform.position;
            searchTime = 2;
        }
        else if (state == State.Chasing) state = State.LastKnownLocation;
        ListenForFootsteps();
        agent.speed = state == State.Chasing ? 4.1f : 2f;
        SetDestinationIfChanged(currentDestination);
        if (!agent.pathPending && agent.remainingDistance <= destinationTolerance)
        {
            if (state == State.Patrolling) SelectPatrolPoint();
            else if (state == State.LastKnownLocation)
            {
                transform.Rotate(0, 75 * Time.deltaTime, 0);
                state = State.Lingering;
                lingerRoutine = StartCoroutine(Linger());
            }
        }
        if (player != null && !player.Hidden && Vector3.Distance(transform.position, target.transform.position) < .85f &&
            !WorldSight.Blocked(transform.position + Vector3.up * .5f, target.transform.position + Vector3.up * .5f, transform, player.transform))
            GoblinDungeon.Instance.Caught();
    }

    bool EnsureNavigation()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        return NavMesh.SamplePosition(transform.position, out var point, 2, agent.areaMask) && agent.Warp(point.position);
    }

    bool RecoverCrown()
    {
        var crown = GoblinDungeon.Instance.Crown;
        if (crown == null) return false;
        // Grab a dropped crown when passing over it, even mid-chase.
        if (crown.Dropped) crown.TryGuardPickup(this);
        // Only the carrier ignores the player. Anyone who can see the thief keeps chasing.
        if (crown.GuardCarrier != this && (!crown.Dropped || CanSeeTarget))
        {
            if (state == State.RecoveringCrown || state == State.ReturningCrown)
            {
                state = State.Patrolling;
                SelectPatrolPoint();
            }
            return false;
        }

        StopLingering();
        bool returning = crown.GuardCarrier == this;
        state = returning ? State.ReturningCrown : State.RecoveringCrown;
        Vector3 destination = returning ? crown.HomePosition : crown.transform.position;
        // The pedestal may not be walkable, so approach its nearest floor point.
        if (NavMesh.SamplePosition(destination, out var hit, 2f, agent.areaMask)) destination = hit.position;
        currentDestination = destination;
        agent.speed = 2f;
        SetDestinationIfChanged(destination);
        if (returning && !agent.pathPending && agent.remainingDistance <= destinationTolerance)
        {
            bool reachedHome = agent.pathStatus == NavMeshPathStatus.PathComplete &&
                               Vector3.Distance(transform.position, destination) <= 1f;
            // If the NavMesh can't reach the pedestal (e.g. a doorway too narrow for the baked
            // agent radius), the guard stops at the closest reachable point. Put the crown
            // back from there instead of carrying it forever.
            bool reachedClosestPoint = agent.pathStatus != NavMeshPathStatus.PathComplete;
            if (!reachedHome && !reachedClosestPoint) return true;
            crown.ReturnHome(this);
            state = State.Patrolling;
            SelectPatrolPoint();
        }
        return true;
    }

    void SetDestinationIfChanged(Vector3 destination)
    {
        if (hasRequestedDestination &&
            (requestedDestination - destination).sqrMagnitude <= .01f &&
            !agent.isPathStale)
            return;

        if (!agent.SetDestination(destination))
        {
            requestedDestination = destination;
            hasRequestedDestination = true;
            Debug.LogWarning($"{name} could not set a navigation destination at {destination}.", this);
            return;
        }

        requestedDestination = destination;
        hasRequestedDestination = true;
    }

    void StopLingering()
    {
        if (lingerRoutine == null) return;
        StopCoroutine(lingerRoutine);
        lingerRoutine = null;
    }

    void ListenForFootsteps()
    {
        if (player != null && player.FootstepId != heardFootstep)
        {
            heardFootstep = player.FootstepId;
            // Walls muffle footsteps; hearing works behind the guard and outside its FOV.
            bool muffled = WorldSight.Blocked(transform.position + Vector3.up,
                player.FootstepPosition + Vector3.up * .4f, transform, player.transform);
            float hearingRadius = player.FootstepRadius * (muffled ? .5f : 1f);
            if (!CanSeeTarget && Vector3.Distance(transform.position, player.FootstepPosition) < hearingRadius)
            {
                StopLingering();
                state = State.LastKnownLocation;
                currentDestination = player.FootstepPosition;
                searchTime = 2;
            }
        }
    }

    IEnumerator FOVRoutine()
    {
        var wait = new WaitForSeconds(.15f);
        while (true) { FieldOfViewCheck(); yield return wait; }
    }

    void FieldOfViewCheck()
    {
        CanSeeTarget = false;
        if (target == null || player == null || player.Hidden) return;
        Vector3 offset = target.transform.position - transform.position;
        float visibleRadius = player.Sneaking ? radius * .65f : radius;
        float distance = offset.magnitude;
        if (distance > visibleRadius) return;
        // Mid-chase the guard is locked on, so the cone no longer applies; up close it senses you.
        bool inView = state == State.Chasing ||
                      distance <= (player.Sneaking ? closeRange * .6f : closeRange) ||
                      Vector3.Angle(transform.forward, offset) <= angle * .5f;
        if (!inView) return;
        // Aim at the player's chest so floor edges and low clutter don't break sight.
        // Crouching lowers the aim point, so low cover only hides a sneaking player.
        float aimHeight = player.Sneaking ? .55f : .9f;
        CanSeeTarget = !WorldSight.Blocked(transform.position + Vector3.up,
            target.transform.position + Vector3.up * aimHeight, transform, player.transform);
    }

    void SelectPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) { currentDestination = transform.position; return; }
        currentDestination = patrolPoints[patrolIndex].position;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    IEnumerator Linger()
    {
        Debug.Log("Start Lingering");

        //rotate 90 degrees
        yield return SmoothTurn(90f, 90f);
        Debug.Log("Turn");

        //Wait 4 seconds
        yield return new WaitForSeconds(3f);

        //rotate 40 degrees
        yield return SmoothTurn(-40f, 60f);
        Debug.Log("Turn");

        //Wait 2 seconds
        yield return new WaitForSeconds(2f); 

        yield return SmoothTurn(100f, 100f);
        Debug.Log("Turn");

        yield return new WaitForSeconds(2f);

        //rotate 20 degrees
        yield return SmoothTurn(20f, 45f);
        Debug.Log("Turn");

        yield return new WaitForSeconds(1f);

        searchTime -= Time.deltaTime;
        state = State.Patrolling;
        lingerRoutine = null;
        SelectPatrolPoint();

        Debug.Log("Stop Lingering");
    }

    IEnumerator SmoothTurn(float angle, float speed)
    {
        Quaternion targetRotation =
            transform.rotation * Quaternion.Euler(0f, angle, 0f);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                speed * Time.deltaTime
            );

            yield return null;
        }

        transform.rotation = targetRotation;
    }
}
