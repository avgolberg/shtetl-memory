using UnityEngine;
public class MiniGameItem : MonoBehaviour, IInteractable
{
    public int miniGameItemID;
    public string uniqueID;
    public string Name;
    public Sprite updatedSprite;

    [SerializeField] private GameObject miniGameUI;

    private bool isCompleted;

    void Awake()
    {
        uniqueID = GlobalHelper.GenerateUniqueID(gameObject);
    }

    public bool CanInteract()
    {
        return !isCompleted;
    }

    public void Interact()
    {
        if (isCompleted)
            return;

        if (miniGameUI == null)
        {
            Debug.LogWarning($"MiniGame UI is missing on {gameObject.name}");
            return;
        }

        GameObject miniGameObject = Instantiate(miniGameUI);
        MiniGameBase miniGame = miniGameObject.GetComponent<MiniGameBase>();

        if (miniGame == null)
        {
            Debug.LogWarning($"MiniGameBase not found on {miniGameObject.name}");
            return;
        }

        miniGame.Open(this);
    }

    public void CompleteMiniGame()
    {
        isCompleted = true;
        UpdateVisualState();
    }

    public void SetCompleted(bool completed)
    {
        isCompleted = completed;
        UpdateVisualState();
    }

    public bool IsCompleted()
    {
        return isCompleted;
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