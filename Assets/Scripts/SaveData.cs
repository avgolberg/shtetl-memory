using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public Collider2D cameraBounding;
    public List<string> collectedItemIds;
    public List<string> openedChestIds;
    public List<InventorySaveData> inventorySaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handinQuestIDs;
}

[System.Serializable]
public class InventorySaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity = 1;
}