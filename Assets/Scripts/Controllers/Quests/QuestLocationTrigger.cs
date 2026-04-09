using UnityEngine;

public class QuestLocationTrigger : MonoBehaviour
{
    [SerializeField] private string locationId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        QuestController.Instance.NotifyLocationReached(locationId);
    }
}
