using UnityEngine;

public class Delivery : MonoBehaviour
{
    [SerializeField] float delay = 0.2f;
    bool hasPackage;
    ParticleSystem particles;

    void Start()
    {
        particles = GetComponent<ParticleSystem>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boost"))
        {
            hasPackage = true;
            particles.Play();
            Destroy(collision.gameObject, delay);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.collider.CompareTag("WorldCollision"))
        {
             particles.Stop();
        }
    }
}
