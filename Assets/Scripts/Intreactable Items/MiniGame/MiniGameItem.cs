using UnityEngine;

public class MiniGameItem : MonoBehaviour, IInteractable
{
    public int miniGameItemID;
    public string uniqueID;
    public string Name;
    public Sprite updatedSprite;
    public Vector3 spawnPosition;
    [SerializeField] private string questObjectiveId; //Optional: Custom quest objective completed with the mini-game
    [SerializeField] private InspectPoint revealOnComplete; //Optional: what the player finds, shown now and on later visits
    [SerializeField] private bool removeOnComplete; //Gone once solved (and after a reload), e.g. Halyna's key box
    private IMiniGame miniGame;
    private bool isCompleted;

    void Awake()
    {
        uniqueID = GlobalHelper.GenerateUniqueID(gameObject);
        miniGame = FindFirstObjectByType<MiniGameRegistry>(FindObjectsInactive.Include)
        ?.GetMiniGame(miniGameItemID);
    }

    void Start()
    {
        var itemData = SaveController.Instance.GetSpawnedItemData(uniqueID);
        if (itemData != null)
        {
            isCompleted = itemData.isCompleted;
            if (itemData.wasSpawned)
                spawnPosition = itemData.spawnPosition;
            UpdateVisualState();
        }
    }

    public bool IsCompleted => isCompleted;
    public bool RemovesOnComplete => removeOnComplete;

    public bool CanInteract()
    {
        if (isCompleted)
            return revealOnComplete != null && revealOnComplete.CanInteract();

        return true;
    }

    public void Interact()
    {
        if (isCompleted)
        {
            revealOnComplete?.Interact();
            return;
        }

        miniGame?.Open(this);
    }

    public void CompleteMiniGame()
    {
        isCompleted = true;
        SaveController.Instance.MarkMiniGameCompleted(miniGameItemID, uniqueID, spawnPosition);
        UpdateVisualState();

        if (!string.IsNullOrEmpty(questObjectiveId))
            QuestController.Instance.CompleteObjective(questObjectiveId);

        revealOnComplete?.Interact();
    }

    private void UpdateVisualState()
    {
        if (isCompleted && removeOnComplete)
        {
            //Destroyed at the end of the frame, after the mini-game has closed
            Destroy(gameObject);
            return;
        }

        if (isCompleted && updatedSprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sprite = updatedSprite;
        }
    }
}