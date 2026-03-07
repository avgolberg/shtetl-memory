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
        if (collision.CompareTag("Item"))
        {
            Item item = collision.GetComponent<Item>();
            if(item != null)
            {
                //Add item inventory
                bool itemAdded = inventoryController.AddItem(collision.gameObject);

                if (itemAdded)
                {
                    SoundEffectManager.Play("CollectItem");
                    item.ShowPopUp();
                    saveController.MarkItemCollected(item.uniqueId);
                    Destroy(collision.gameObject);
                }
            }
        }
    }
}
