using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField, Range(0, 1)] float parallaxEffect;
    
    float startPosition, length;

    void Start()
    {
        startPosition = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        float distance = cameraTransform.position.x * parallaxEffect;
        float movement = cameraTransform.position.x * (1 - parallaxEffect);
        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);

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
