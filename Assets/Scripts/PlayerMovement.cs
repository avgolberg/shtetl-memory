using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float accel = 12f;

    Vector2 moveInput;
    Rigidbody2D rb;
    Animator animator;

    void Awake()
    {
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
            rb.linearVelocity = Vector2.zero;
            animator.SetFloat("speed", 0);
            return;
        }

        Move();
        FlipSprite();
    }

    void Move()
    {        
        float targetX = moveInput.x * moveSpeed;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * Time.deltaTime);
        if (Mathf.Abs(newX) < 0.05f) newX = 0f;

        Vector2 playerVelocity = new Vector2(newX, rb.linearVelocity.y);
        rb.linearVelocity = playerVelocity;

        float speed01 = Mathf.Abs(rb.linearVelocity.x) / moveSpeed; // 0..1
        animator.SetFloat("speed", speed01);
    }

    void FlipSprite()
    {
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > Mathf.Epsilon;
        if (isMoving)
            transform.localScale = new Vector2(Mathf.Sign(rb.linearVelocity.x), 1f);
    }
}
