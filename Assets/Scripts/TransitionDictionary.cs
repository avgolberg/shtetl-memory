using System.Collections.Generic;
using UnityEngine;

public class TransitionDictionary : MonoBehaviour
{
    public List<MapTransition> transitionPrefabs;
    private Dictionary<int, GameObject> transitionDictionary;

    private void Awake()
    {
        transitionDictionary = new Dictionary<int, GameObject>();

        for(int i = 0; i < transitionPrefabs.Count; i++)
        {
            if(transitionPrefabs[i] != null)
            {
                transitionPrefabs[i].ID = i + 1;
            }
        }

        foreach(MapTransition t in transitionPrefabs)
        {
            transitionDictionary[t.ID] = t.gameObject;
        }
    }

    public GameObject GetTransitionPrefab(int transitionID)
    {
        transitionDictionary.TryGetValue(transitionID, out GameObject prefab);
        if(prefab == null)
        {
            Debug.LogWarning($"Transition with ID {transitionID} not found in dictionary");
        }
        return prefab;
    }
}
