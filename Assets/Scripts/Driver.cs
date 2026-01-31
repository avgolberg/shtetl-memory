using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Driver : MonoBehaviour
{
    [SerializeField] float currentSpeed = 5f;
    [SerializeField] float rotateSpeed = 150f;
    [SerializeField] float boostSpeed = 7f;
    [SerializeField] float regularSpeed = 5f;

    [SerializeField] TMP_Text boostText;

    float move, rotate, moveAmount, rotateAmount;

    private Vector2 input;
    private Rigidbody2D rb;
    public float speed = 4f;

        void Start()
    {rb = GetComponent<Rigidbody2D>();
        boostText.gameObject.SetActive(false);
    }
    void Update()
    {
        input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y = 1;
        if (Keyboard.current.sKey.isPressed) input.y = -1;
        if (Keyboard.current.dKey.isPressed) input.x = 1;
        if (Keyboard.current.aKey.isPressed) input.x = -1;

        input = input.normalized;
        
        rb.MovePosition(rb.position + input * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boost"))
        {
            currentSpeed = boostSpeed;
            boostText.gameObject.SetActive(true);
            Destroy(collision.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.collider.CompareTag("WorldCollision"))
        {
            currentSpeed = regularSpeed;
            boostText.gameObject.SetActive(false);
        }
    }
}
