using System.Collections.Generic;
using UnityEngine;

public class ItemDictionary : MonoBehaviour
{
    public List<Item> itemPrefabs;
    private Dictionary<int, GameObject> itemDictionary;

    private void Awake()
    {
        itemDictionary = new Dictionary<int, GameObject>();

        for(int i = 0; i < itemPrefabs.Count; i++)
        {
            if(itemPrefabs[i] != null)
            {
                int firstIndex = itemPrefabs.IndexOf(itemPrefabs[i]);
                if(firstIndex != i)
                {
                    //A second entry would renumber the item and break the quests that use its first ID
                    Debug.LogError($"Item {itemPrefabs[i].name} is listed twice in ItemDictionary (IDs {firstIndex + 1} and {i + 1}). Run Shtetl > Fix Flower Quest.");
                    continue;
                }
                itemPrefabs[i].itemId = i + 1;
            }
        }

        foreach(Item item in itemPrefabs)
        {
            itemDictionary[item.itemId] = item.gameObject;
        }
    }

    public GameObject GetItemPrefab(int itemID)
    {
        itemDictionary.TryGetValue(itemID, out GameObject prefab);
        if(prefab == null)
        {
            Debug.LogWarning($"Item with ID {itemID} not found in dictionary");
        }
        return prefab;
    }
}
