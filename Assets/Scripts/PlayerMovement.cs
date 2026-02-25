using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float accel = 12f;
    private float stopThreshold = 0.05f;
    private bool playingFootsteps = false;
    public float footstepSpeed = 0.5f;
    Vector2 moveInput;
    Rigidbody2D rb;
    private Vector3 baseScale;
    Animator animator;

    void Awake()
    {
        baseScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        if (PauseController.IsGamePaused)
        {
            if(rb.linearVelocity != Vector2.zero)
            {
                rb.linearVelocity = Vector2.zero;
                animator.SetFloat("speed", 0);
                StopFootsteps();
            }
            return;
        }

        Move();
        FlipSprite();
    }

    void Move()
    {
        float targetX = moveInput.x * moveSpeed;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * Time.deltaTime);
        if (Mathf.Abs(newX) < stopThreshold) newX = 0f;

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        float speed01 = Mathf.Abs(rb.linearVelocity.x) / moveSpeed; // 0..1
        animator.SetFloat("speed", speed01);
        
        bool isMoving = Mathf.Abs(newX) > stopThreshold; 

        if (isMoving && !playingFootsteps)
        {
            StartFootsteps();
        }
        else if (!isMoving && playingFootsteps)
        {
            StopFootsteps();
        }
    }

    void FlipSprite()
    {
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > Mathf.Epsilon;
        if (isMoving)
            transform.localScale = new Vector2(Mathf.Sign(rb.linearVelocity.x) * Mathf.Abs(baseScale.x), baseScale.y);
    }
    void StartFootsteps()
    {
        playingFootsteps = true;
        InvokeRepeating(nameof(PlayFootstep), 0f, footstepSpeed);
    }

    void StopFootsteps()
    {
        playingFootsteps = false;
        CancelInvoke(nameof(PlayFootstep));
    }

    void PlayFootstep()
    {
        SoundEffectManager.Play("Footstep", true);
    }
}
