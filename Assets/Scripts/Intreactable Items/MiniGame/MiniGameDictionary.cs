using System.Collections.Generic;
using UnityEngine;

public class MiniGameItemDictionary : MonoBehaviour
{
    public List<MiniGameItem> miniGameItemPrefabs;
    private Dictionary<int, GameObject> miniGameItemDictionary;

    private void Awake()
    {
        miniGameItemDictionary = new Dictionary<int, GameObject>();

        for (int i = 0; i < miniGameItemPrefabs.Count; i++)
        {
            if (miniGameItemPrefabs[i] != null)
            {
                miniGameItemPrefabs[i].miniGameItemID = i + 1;
            }
        }

        foreach (MiniGameItem item in miniGameItemPrefabs)
        {
            miniGameItemDictionary[item.miniGameItemID] = item.gameObject;
        }
    }

    public GameObject GetMiniGameItemPrefab(int miniGameItemID)
    {
        miniGameItemDictionary.TryGetValue(miniGameItemID, out GameObject prefab);

        if (prefab == null)
        {
            Debug.LogWarning($"MiniGameItem with ID {miniGameItemID} not found in dictionary");
        }

        return prefab;
    }
}
