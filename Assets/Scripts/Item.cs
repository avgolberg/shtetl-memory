using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public int itemId;
    public string uniqueId;
    public string Name;
    public int quantity = 1;
    public bool IsCollected { get; private set; }
    private TMP_Text quantityText;
    private SaveController saveController;
    private void Awake()
    {
        uniqueId = GlobalHelper.GenerateUniqueID(gameObject);
        quantityText = GetComponentInChildren<TMP_Text>();
        saveController = FindFirstObjectByType<SaveController>();
        UpdateQuantityDisplay();
    }

    void Start()
    {
        if (saveController != null && saveController.IsItemCollected(uniqueId))
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
}
