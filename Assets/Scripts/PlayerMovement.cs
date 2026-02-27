using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float accel = 12f;
    [SerializeField] float stopThreshold = 0.05f;

    [Header("Footsteps")]
    [SerializeField] float minSpeedForStep = 0.5f;     
    [SerializeField] float stepIntervalWalk = 0.7f;  
    [SerializeField] float stepIntervalRun = 0.45f;

    Vector2 moveInput;
    Rigidbody2D rb;
    Animator animator;
    Vector3 baseScale;

    float stepTimer;
   
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
                rb.linearVelocity = Vector2.zero;

            animator.SetFloat("speed", 0f);
            stepTimer = 0f;
            return;
        }

        FlipSprite();
    }

    void FixedUpdate()
    {
        if (PauseController.IsGamePaused)
            return;

        Move();
        UpdateFootsteps();
    }

    void Move()
    {
        float targetX = moveInput.x * moveSpeed;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * Time.fixedDeltaTime);
        if (Mathf.Abs(newX) < stopThreshold) newX = 0f;

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        float speed01 = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / moveSpeed);
        animator.SetFloat("speed", speed01);
    }

    void UpdateFootsteps()
    {
        bool hasInput = Mathf.Abs(moveInput.x) > 0.01f;

        float speedAbs = Mathf.Abs(rb.linearVelocity.x);
        bool canStep = hasInput && speedAbs > minSpeedForStep;

        if (!canStep)
        {
            stepTimer = 0f;
            return;
        }

        float t = Mathf.Clamp01(speedAbs / moveSpeed);
        float interval = Mathf.Lerp(stepIntervalWalk, stepIntervalRun, t);

        stepTimer += Time.fixedDeltaTime;
        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            PlayFootstep();
        }
    }

    void FlipSprite()
    {
        float vx = rb.linearVelocity.x;
        if (Mathf.Abs(vx) > 0.001f)
            transform.localScale = new Vector2(Mathf.Sign(vx) * Mathf.Abs(baseScale.x), baseScale.y);
    }
    
    void PlayFootstep()
    {
        if (PauseController.IsGamePaused) return;
        SoundEffectManager.Play("Footstep", true);
    }
}
