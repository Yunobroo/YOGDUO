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
    enum State { Patrolling, Chasing, LastKnownLocation }
    State state;
    Vector3 currentDestination;
    int patrolIndex;
    float searchTime;
    Player player;
    int heardFootstep;

    void Start()
    {
        if (target == null) target = GameObject.FindGameObjectWithTag(targetTag);
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (target != null) player = target.GetComponent<Player>();
        SelectPatrolPoint();
        StartCoroutine(FOVRoutine());
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (GoblinDungeon.Instance == null || !GoblinDungeon.Instance.Playing) { agent.isStopped = true; return; }
        agent.isStopped = false;
        if (CanSeeTarget)
        {
            state = State.Chasing;
            currentDestination = target.transform.position;
            searchTime = 2;
        }
        else if (state == State.Chasing) state = State.LastKnownLocation;
        if (player != null && player.FootstepId != heardFootstep)
        {
            heardFootstep = player.FootstepId;
            // Walls muffle footsteps; hearing works behind the guard and outside its FOV.
            bool muffled = Physics.Linecast(transform.position + Vector3.up,
                player.FootstepPosition + Vector3.up * .4f, visionBlockMask);
            float hearingRadius = player.FootstepRadius * (muffled ? .5f : 1f);
            if (!CanSeeTarget && Vector3.Distance(transform.position, player.FootstepPosition) < hearingRadius)
            {
                state = State.LastKnownLocation;
                currentDestination = player.FootstepPosition;
                searchTime = 2;
            }
        }
        agent.speed = state == State.Chasing ? 4.1f : 2f;
        agent.SetDestination(currentDestination);
        if (!agent.pathPending && agent.remainingDistance <= destinationTolerance)
        {
            if (state == State.Patrolling) SelectPatrolPoint();
            else if (state == State.LastKnownLocation)
            {
                transform.Rotate(0, 75 * Time.deltaTime, 0);
                searchTime -= Time.deltaTime;
                if (searchTime <= 0) { state = State.Patrolling; SelectPatrolPoint(); }
            }
        }
        if (player != null && !player.Hidden && Vector3.Distance(transform.position, target.transform.position) < .85f &&
            !Physics.Linecast(transform.position + Vector3.up * .5f, target.transform.position + Vector3.up * .5f, visionBlockMask))
            GoblinDungeon.Instance.Caught();
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
}
