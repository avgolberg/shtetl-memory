using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    private InventoryController inventoryController;
    private SaveController saveController;
    void Start()
    {
        inventoryController = FindFirstObjectByType<InventoryController>();
        saveController = FindFirstObjectByType<SaveController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Item")) return;

        Item item = collision.GetComponent<Item>();
        if (item == null) return;

        if (!item.TryCollect()) return;
        
        bool itemAdded = inventoryController.AddItem(collision.gameObject);
        if (itemAdded)
        {
            SoundEffectManager.Play("CollectItem");
            item.ShowPopUp();
            if (item.ShouldTrackCollection)
                saveController.MarkItemCollected(item.uniqueId);
            Destroy(collision.gameObject);
        }
    }
}
