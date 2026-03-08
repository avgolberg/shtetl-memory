using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private List<string> collectedItemIds = new();
    private HashSet<string> collectedItemIdsSet = new();
    private List<string> openedChestIds = new();
    private HashSet<string> openedChestIdsSet = new();

    void Awake()
    {
        //Define save location
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        LoadGame();
    }

    public void SaveGame()
    {
        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position,
            cameraBounding = FindFirstObjectByType<CinemachineConfiner2D>().BoundingShape2D,
            inventorySaveData = InventoryController.Instance.GetInventoryItems(),
            collectedItemIds = collectedItemIds,
            openedChestIds = openedChestIds,
            questProgressData = QuestController.Instance.activateQuests,
            handinQuestIDs = QuestController.Instance.handinQuestIDs
        };

        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            FindFirstObjectByType<CinemachineConfiner2D>().BoundingShape2D = saveData.cameraBounding;
            player.transform.position = saveData.playerPosition;

            InventoryController.Instance.SetInventoryItems(saveData.inventorySaveData);

            LoadCollectedItemIds(saveData.collectedItemIds);
            LoadOpenedChestIds(saveData.openedChestIds);
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
            QuestController.Instance.handinQuestIDs = saveData.handinQuestIDs;
        }
        else
        {
            SaveGame();
            InventoryController.Instance.SetInventoryItems(new List<InventorySaveData>()); //
        }
    }

    public void MarkItemCollected(string uniqueId)
    {
        if (collectedItemIdsSet.Add(uniqueId))
        {
            collectedItemIds.Add(uniqueId);
        }
    }

    public bool IsItemCollected(string uniqueId)
    {
        return collectedItemIdsSet.Contains(uniqueId);
    }

    public void LoadCollectedItemIds(List<string> ids)
    {
        collectedItemIds = ids ?? new List<string>();
        collectedItemIdsSet = new HashSet<string>(collectedItemIds);
    }

    public void MarkChestOpened(string uniqueId)
    {
        if (openedChestIdsSet.Add(uniqueId))
        {
            openedChestIds.Add(uniqueId);
        }
    }

    public bool IsChestOpened(string uniqueId)
    {
        return openedChestIdsSet.Contains(uniqueId);
    }

    public void LoadOpenedChestIds(List<string> ids)
    {
        openedChestIds = ids ?? new List<string>();
        openedChestIdsSet = new HashSet<string>(openedChestIds);
    }
}
