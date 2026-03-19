using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public int itemId;
    public string uniqueId;
    public string Name;
    public int quantity = 1;
    [SerializeField] private ItemType itemType;
    public ItemType ItemType => itemType;
    [SerializeField] private bool requiresActiveQuest = false;

    private bool canBeCollected = true;
    private bool isBeingCollected;
    private bool shouldTrackCollection = true;
    public bool ShouldTrackCollection => shouldTrackCollection;

    private TMP_Text quantityText;
    private void Awake()
    {
        uniqueId = GlobalHelper.GenerateUniqueID(gameObject);
        quantityText = GetComponentInChildren<TMP_Text>();
        UpdateQuantityDisplay();
    }

    void Start()
    {
        if (SaveController.Instance.IsItemCollected(uniqueId))
        {
            Destroy(gameObject);
        }
    }

    public void UpdateQuantityDisplay()
    {
        if (quantityText != null)
        {
            quantityText.text = quantity > 1 ? quantity.ToString() : "";
        }
    }
    public void AddToStack(int amount = 1)
    {
        quantity += amount;
        UpdateQuantityDisplay();
    }

    public int RemoveFromStack(int amount = 1)
    {
        int removed = Mathf.Min(amount, quantity);
        quantity -= removed;
        UpdateQuantityDisplay();
        return removed;
    }

    public GameObject CloneItem(int newQuantity, Transform transform)
    {
        GameObject clone = Instantiate(gameObject, transform);
        Item cloneItem = clone.GetComponent<Item>();
        cloneItem.quantity = newQuantity;
        cloneItem.UpdateQuantityDisplay();
        return clone;
    }

    public virtual void ShowPopUp()
    {
        Sprite itemIcon = GetComponent<Image>().sprite;
        if (ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPickup(Name, itemIcon);
        }
    }

    public void EnableCollection(float delay)
    {
        canBeCollected = false;
        Invoke(nameof(SetCollectable), delay);
    }

    private void SetCollectable()
    {
        canBeCollected = true;
    }

    public bool TryCollect()
    {
        if (!canBeCollected || isBeingCollected)
            return false;

        if (requiresActiveQuest)
            return QuestController.Instance.CanCollectObjectiveItem(itemType);

        isBeingCollected = true;
        return true;
    }

    public void SetTrackCollection(bool value)
    {
        shouldTrackCollection = value;
    }
}

public enum ItemType { Coin, Flower, Key, Note, Custom}
