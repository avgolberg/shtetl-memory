using UnityEngine;

public class FreezeWhenOffscreen : MonoBehaviour
{
    [SerializeField] float extraMargin = 2f;
    [SerializeField] bool disableAnimator = true;

    Camera cam;
    Rigidbody2D rb;
    Animator animator;
    bool frozen;

    void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!cam) return;

        bool visible = IsInCameraBounds(cam, extraMargin);

        if (!visible && !frozen) Freeze();
        else if (visible && frozen) Unfreeze();
    }

    bool IsInCameraBounds(Camera cam, float margin)
    {
        var min = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        var max = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        return transform.position.x >= min.x - margin &&
               transform.position.x <= max.x + margin &&
               transform.position.y >= min.y - margin &&
               transform.position.y <= max.y + margin;
    }

    void Freeze()
    {
        frozen = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        if (disableAnimator && animator) animator.speed = 0f;
    }

    void Unfreeze()
    {
        frozen = false;
        rb.simulated = true;
        if (disableAnimator && animator) animator.speed = 1f;
    }
}