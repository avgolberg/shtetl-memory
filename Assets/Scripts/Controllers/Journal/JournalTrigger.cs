using UnityEngine;

public class JournalTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private string topicId;
    [SerializeField] private int stageIndex;

    public bool CanInteract()
    {
        return JournalController.Instance.CanUnlockOrUpdateTopic(topicId, stageIndex);
    }

    public void Interact()
    {
        JournalController.Instance.UnlockOrUpdateTopic(topicId, stageIndex);
    }
}