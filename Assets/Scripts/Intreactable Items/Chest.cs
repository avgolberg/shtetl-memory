using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    public bool IsOpened { get; private set; }
    public string ChestID { get; private set; }
    [SerializeField] GameObject itemPrefab; //Item that chest drops
    [SerializeField] Sprite openedSprite;
    [SerializeField] float dropDistance = 5f;
    [SerializeField] float collectionDelay = 0.3f;
    private SaveController saveController;

    void Awake()
    {
        saveController = FindFirstObjectByType<SaveController>();
    }

    void Start()
    {
        ChestID ??= GlobalHelper.GenerateUniqueID(gameObject);
        if (saveController != null)
        {
            IsOpened = saveController.IsChestOpened(ChestID);
        }
    }

    public bool CanInteract()
    {
        return !IsOpened;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        OpenChest();
    }

    private void OpenChest()
    {
        SetOpened(true);
        SoundEffectManager.Play("ChestOpened");

        if (itemPrefab)
        {
            ItemDropSpawner.SpawnItem(itemPrefab, gameObject, dropDistance, collectionDelay);
        }
    }
    public void SetOpened(bool opened)
    {
        IsOpened = opened;

        if (IsOpened)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
            saveController.MarkChestOpened(ChestID);
        }
    }
}