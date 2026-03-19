using UnityEngine;
using UnityEngine.EventSystems;

public class HiddenObjectItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    private bool isFound = false;

    private HiddenObjectGameController gameController;
    private ItemDictionary itemDictionary;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool IsFound => isFound;

    public void Init(HiddenObjectGameController controller)
    {
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        gameController = controller;
    }

   public void OnPointerClick(PointerEventData eventData)
    {
        if (gameController == null || isFound) return;
        gameController.TrySelectItem(this);
    }

    public void MarkAsFound()
    {
        isFound = true;
        gameObject.SetActive(false);

        var itemPrefab = itemDictionary.GetItemPrefab(itemId);
        InventoryController.Instance.AddItem(itemPrefab);
        ItemPickupUIController.Instance.ShowItemPickup(displayName, icon);
    }
}