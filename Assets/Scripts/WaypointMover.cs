using System.Collections;
using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    
    [SerializeField] Transform waypointParent;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float waitTime = 2f;
    [SerializeField] bool loopWaypoints = true;
    [SerializeField] float arriveDistance = 0.1f;
    private Transform[] waypoints;
    private int currentWaypointIndex;
    private bool isWaiting;

    Animator animator;
    Rigidbody2D rb;
    private Vector3 baseScale;
    static readonly int isWalking = Animator.StringToHash("isWalking");

    void Awake()
    {
        baseScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        waypoints = new Transform[waypointParent.childCount];
        for (int i = 0; i < waypointParent.childCount; i++)
        {
            waypoints[i] = waypointParent.GetChild(i);
        }
        
        animator.SetBool(isWalking, false);
    }

    void FixedUpdate()
    {
        if (PauseController.IsGamePaused || isWaiting)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool(isWalking, false);
            return;
        }

        MoveToWaypoint();
        animator.SetBool(isWalking, rb.linearVelocity.sqrMagnitude > 0.0001f);
    }

    void MoveToWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];

        Vector2 currentPos = rb.position;
        Vector2 targetPos = target.position;

        float dx = targetPos.x - currentPos.x;

        if (Mathf.Abs(dx) <= arriveDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StartCoroutine(WaitAtWaypoint());
            return;
        }

        rb.linearVelocity = new Vector2(Mathf.Sign(dx) * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector2(Mathf.Sign(rb.linearVelocity.x) * Mathf.Abs(baseScale.x), baseScale.y);
    }
    IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        animator.SetBool(isWalking, false);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(waitTime);

        // If looping is enabled: increment currentWaypointIndex and wrap around if needed
        // If not looping: increment currentWaypointIndex but don't exceed last waypoint
        currentWaypointIndex = loopWaypoints ? (currentWaypointIndex + 1) % waypoints.Length : Mathf.Min(currentWaypointIndex + 1, waypoints.Length - 1);

        isWaiting = false;
    }
}
