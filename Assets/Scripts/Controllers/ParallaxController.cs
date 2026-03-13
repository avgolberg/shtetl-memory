using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField, Range(0, 1)] float parallaxEffect;
    [SerializeField] private bool enableLooping = true;

    float startPosition, length;

    void Start()
    {
        startPosition = transform.position.x;

        if (enableLooping)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                length = sr.bounds.size.x;
            }
            else
            {
                Debug.LogWarning($"[{name}] Looping is enabled, but no SpriteRenderer found.", this);
                enableLooping = false;
            }
        }
    }

    void FixedUpdate()
    {
        float cameraX = cameraTransform.position.x;
        float distance = cameraX * parallaxEffect;

        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);

        if (!enableLooping)
            return;

        float movement = cameraTransform.position.x * (1 - parallaxEffect);

        //if background reached the end of its length adjust its position for infinite scrolling
        if (movement > startPosition + length)
        {
            startPosition += length;
        }
        else if (movement < startPosition - length)
        {
            startPosition -= length;
        }
    }
}
