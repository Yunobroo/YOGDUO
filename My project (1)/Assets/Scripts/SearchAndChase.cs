using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Adapted from the supplied SearchAndChase: patrol, chase, then last known location.
public class SearchAndChase : MonoBehaviour
{
    [Header("FOV Size")]
    [Range(0, 360)] public float angle = 85;
    public float radius = 7;
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
    int patrolIndex;
    float searchTime;
    Player player;
    int heardFootstep;
    Coroutine lingerRoutine;

    void Start()
    {
        if (target == null) target = GameObject.FindGameObjectWithTag(targetTag);
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        EnsureNavigation();
        if (target != null) player = target.GetComponent<Player>();
        SelectPatrolPoint();
        StartCoroutine(FOVRoutine());
    }

    void Update()
    {
        if (!EnsureNavigation()) return;
        if (GoblinDungeon.Instance == null || !GoblinDungeon.Instance.Playing) { agent.isStopped = true; return; }
        agent.isStopped = false;
        if (RecoverCrown()) return;
        if (CanSeeTarget)
        {
            StopLingering();
            state = State.Chasing;
            currentDestination = target.transform.position;
            searchTime = 2;
        }
        else if (state == State.Chasing) state = State.LastKnownLocation;
        ListenForFootsteps();
        agent.speed = state == State.Chasing ? 4.1f : 2f;
        agent.SetDestination(currentDestination);
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
            !Physics.Linecast(transform.position + Vector3.up * .5f, target.transform.position + Vector3.up * .5f, visionBlockMask))
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
        if (crown.GuardCarrier != this && !crown.Dropped)
        {
            if (state == State.RecoveringCrown || state == State.ReturningCrown)
            {
                state = State.Patrolling;
                SelectPatrolPoint();
            }
            return false;
        }

        StopLingering();
        if (crown.Dropped) crown.TryGuardPickup(this);
        bool returning = crown.GuardCarrier == this;
        state = returning ? State.ReturningCrown : State.RecoveringCrown;
        Vector3 destination = returning ? crown.HomePosition : crown.transform.position;
        // The pedestal may not be walkable, so approach its nearest floor point.
        if (NavMesh.SamplePosition(destination, out var hit, 2f, agent.areaMask)) destination = hit.position;
        currentDestination = destination;
        agent.speed = 2f;
        agent.SetDestination(destination);
        if (returning && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.remainingDistance <= destinationTolerance && Vector3.Distance(transform.position, destination) <= 1f)
        {
            crown.ReturnHome(this);
            state = State.Patrolling;
            SelectPatrolPoint();
        }
        return true;
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
            bool muffled = Physics.Linecast(transform.position + Vector3.up,
                player.FootstepPosition + Vector3.up * .4f, visionBlockMask);
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
        if (offset.magnitude > visibleRadius || Vector3.Angle(transform.forward, offset) > angle * .5f) return;
        CanSeeTarget = !Physics.Linecast(transform.position + Vector3.up, target.transform.position + Vector3.up * .4f, visionBlockMask);
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
