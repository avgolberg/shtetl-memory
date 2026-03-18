using UnityEngine;

public class MiniGameItem : MonoBehaviour, IInteractable
{
    public int miniGameItemID;
    public string uniqueID;
    public string Name;
    public Sprite updatedSprite;
    public Vector3 spawnPosition;
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
        if (SaveController.Instance.IsItemSpawned(uniqueID))
        {
            var itemData = SaveController.Instance.GetSpawnedItemData(uniqueID);
            isCompleted = itemData.isCompleted;
            spawnPosition = itemData.spawnPosition;
            UpdateVisualState();
        }
    }

    public bool CanInteract()
    {
        return !isCompleted;
    }

    public void Interact()
    {
        if (isCompleted)
            return;

        miniGame?.Open(this);
    }

    public void CompleteMiniGame()
    {
        isCompleted = true;
        SaveController.Instance.MarkMiniGameCompleted(miniGameItemID, uniqueID, spawnPosition);
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (isCompleted && updatedSprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sprite = updatedSprite;
        }
    }
}