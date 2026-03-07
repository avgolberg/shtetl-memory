using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private List<string> collectedItemIds = new();
    private HashSet<string> collectedItemIdsSet = new();

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

    public List<string> GetCollectedItemIds()
    {
        return collectedItemIds;
    }

    public void LoadCollectedItemIds(List<string> ids)
    {
        collectedItemIds = ids ?? new List<string>();
        collectedItemIdsSet = new HashSet<string>(collectedItemIds);
    }
}
