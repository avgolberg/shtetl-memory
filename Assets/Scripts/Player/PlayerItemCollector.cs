using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Item")) return;

        Item item = collision.GetComponent<Item>();
        if (item == null) return;

        if (!item.TryCollect()) return;
        
        bool itemAdded = InventoryController.Instance.AddItem(collision.gameObject);
        if (itemAdded)
        {
            SoundEffectManager.Play("CollectItem");
            item.ShowPopUp();
            if (item.ShouldTrackCollection)
                SaveController.Instance.MarkItemCollected(item.uniqueId);
            Destroy(collision.gameObject);
        }
    }
}
