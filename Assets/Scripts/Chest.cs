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
    private GameObject player;

    void Awake()
    {
        saveController = FindFirstObjectByType<SaveController>();
        player = GameObject.FindGameObjectWithTag("Player");
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
            GameObject droppedItem = Instantiate(itemPrefab, GetSpawnPosition(), Quaternion.identity);
            Item item = droppedItem.GetComponent<Item>();
            item.SetTrackCollection(false);
            item.EnableCollection(collectionDelay);
            droppedItem.GetComponent<BounceEffect>().StartBounce();
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Transform playerTransform = player.transform;
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        float playerHalfHeight = playerCollider.bounds.extents.y;

        Vector3 basePos = GetGroundSpawnBase();
        Vector3 horizontalDir = basePos.x >= playerTransform.position.x ? Vector3.right : Vector3.left;

        if (Mathf.Abs(basePos.x - playerTransform.position.x) < 0.1f)
        {
            horizontalDir = Random.value > 0.5f ? Vector3.right : Vector3.left;
        }

        return basePos + horizontalDir * dropDistance + Vector3.up * playerHalfHeight;
    }

    private Vector3 GetGroundSpawnBase()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return new Vector3(transform.position.x, sr.bounds.min.y, transform.position.z);
        }

        return transform.position;
    }

    public void SetOpened(bool opened)
    {
        if (IsOpened = opened)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
            saveController.MarkChestOpened(ChestID);
        }
    }
}
