using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string currentLocationID;
    public List<string> collectedItemIds;
    public List<string> openedChestIds;
    public List<InventorySaveData> inventorySaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handinQuestIDs;
    public float sfxVolume;
    public float musicVolume;
}

[System.Serializable]
public class InventorySaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity = 1;
}