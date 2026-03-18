using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }
    private string saveLocation;
    private Location currentLocation;
    private List<string> collectedItemIds = new();
    private HashSet<string> collectedItemIdsSet = new();
    private List<string> openedChestIds = new();
    private HashSet<string> openedChestIdsSet = new();
    private List<MiniGameItemSaveData> spawnedItems = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        LoadGame();
    }

    public void SaveGame()
    {
        SaveData sd = new SaveData();

        sd.playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        sd.currentLocationID = currentLocation.LocationID;
        sd.inventorySaveData = InventoryController.Instance.GetInventoryItems();
        sd.spawnedItemSaveData = spawnedItems;
        sd.collectedItemIds = collectedItemIds;
        sd.openedChestIds = openedChestIds;
        sd.questProgressData = QuestController.Instance.activeQuests;
        sd.handinQuestIDs = QuestController.Instance.handinQuestIDs;
        sd.sfxVolume = SoundEffectManager.SFXVolume;
        sd.musicVolume = SoundEffectManager.MusicVolume;

        File.WriteAllText(saveLocation, JsonUtility.ToJson(sd));
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

            Location[] locations = FindObjectsByType<Location>(FindObjectsSortMode.None);
            Location targetLocation = locations.FirstOrDefault(l => l.LocationID == saveData.currentLocationID);
            SetCurrentLocation(targetLocation);
            FindFirstObjectByType<CinemachineConfiner2D>().BoundingShape2D = targetLocation.MapBoundary;

            SoundEffectManager.InitializeVolumes(saveData.sfxVolume, saveData.musicVolume);
            SoundEffectManager.PlayMusic(targetLocation.MusicClip, targetLocation.MusicVolume, targetLocation.MusicFadeDuration);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = saveData.playerPosition;

            InventoryController.Instance.SetInventoryItems(saveData.inventorySaveData);
            LoadSpawnedItems(saveData.spawnedItemSaveData);
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

    public void SetCurrentLocation(Location location)
    {
        if (location == null) return;
        currentLocation = location;
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

    public void MarkItemSpawned(int miniGameId, string uniqueId, Vector3 spawnPos)
    {
        if (spawnedItems == null)
            spawnedItems = new List<MiniGameItemSaveData>();

        var existingItem = spawnedItems.Find(i => i.uniqueID == uniqueId);

        if (existingItem == null)
        {
            spawnedItems.Add(new MiniGameItemSaveData
            {
                miniGameItemID = miniGameId,
                uniqueID = uniqueId,
                wasSpawned = true,
                isCompleted = false,
                spawnPosition = spawnPos
            });
        }
        else
        {
            existingItem.wasSpawned = true;
            existingItem.isCompleted = false;
            existingItem.spawnPosition = spawnPos;
        }
    }
    public void MarkMiniGameCompleted(int miniGameId, string uniqueId, Vector3 spawnPos)
    {
        if (spawnedItems == null)
            spawnedItems = new List<MiniGameItemSaveData>();

        var itemData = spawnedItems.Find(i => i.uniqueID == uniqueId);

        if (itemData == null)
        {
            spawnedItems.Add(new MiniGameItemSaveData
            {
                miniGameItemID = miniGameId,
                uniqueID = uniqueId,
                wasSpawned = true,
                isCompleted = true,
                spawnPosition = spawnPos
            });
        }
        else
        {
            itemData.isCompleted = true;
        }
    }

    public MiniGameItemSaveData GetSpawnedItemData(string uniqueId)
    {
        if (spawnedItems == null)
            spawnedItems = new List<MiniGameItemSaveData>();

        return spawnedItems.Find(i => i.uniqueID == uniqueId);
    }

    public bool IsItemSpawned(string uniqueId)
    {
        var itemData = GetSpawnedItemData(uniqueId);
        return itemData != null && itemData.wasSpawned;
    }

    public bool IsMiniGameCompleted(string uniqueId)
    {
        var itemData = GetSpawnedItemData(uniqueId);
        return itemData != null && itemData.isCompleted;
    }

    public void LoadSpawnedItems(List<MiniGameItemSaveData> items)
    {
        spawnedItems = items ?? new List<MiniGameItemSaveData>();

        foreach (var spawnedItem in spawnedItems)
        {
            if (!spawnedItem.wasSpawned) continue;

            var prefab = FindAnyObjectByType<MiniGameItemDictionary>()?.GetMiniGameItemPrefab(spawnedItem.miniGameItemID);
            if (prefab == null) continue;
            
            ItemDropSpawner.SpawnItemAtposition(prefab, spawnedItem.spawnPosition);
        }
    }
}
