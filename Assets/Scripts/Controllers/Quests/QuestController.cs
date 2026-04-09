using System.Collections.Generic;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }
    public List<QuestProgress> activeQuests = new();
    public List<string> handinQuestIDs = new();
    [SerializeField] Sprite questIcon;

    [SerializeField] private Transform listContent;
    [SerializeField] private EntryBlockUI entryPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InventoryController.Instance.OnInventoryChanged += CheckInventoryForQuests;
    }

    public void AcceptQuest(Quest quest)
    {
        if (IsQuestActive(quest.questID)) return;

        activeQuests.Add(new QuestProgress(quest));
        if (ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPickup("New quest: \n" + quest.questName, questIcon);
        }

        CheckInventoryForQuests();
        UpdateUI();
    }

    public bool CanCollectObjectiveItem(ItemType itemType)
    {
        foreach (var questProgress in activeQuests)
        {
            foreach (var objective in questProgress.objectives)
            {
                if (objective.IsCompleted)
                    continue;

                if (objective.type != ObjectiveType.CollectItem)
                    continue;

                if (objective.targetItemType == itemType)
                    return true;
            }
        }
        return false;
    }

    public bool IsQuestActive(string questID) => activeQuests.Exists(q => q.QuestID == questID);
    public void CheckInventoryForQuests()
    {
        Dictionary<int, int> itemCounts = InventoryController.Instance.GetItemCounts();

        foreach (QuestProgress quest in activeQuests)
        {
            foreach (QuestObjective questObjective in quest.objectives)
            {
                if (questObjective.type != ObjectiveType.CollectItem) continue;
                if (!int.TryParse(questObjective.objectiveID, out int itemID)) continue;

                int newAmount = itemCounts.TryGetValue(itemID, out int count) ? Mathf.Min(count, questObjective.requiredAmount) : 0;

                if (questObjective.currentAmount != newAmount)
                {
                    questObjective.currentAmount = newAmount;
                }
            }
        }
        UpdateUI();
    }

    public bool IsQuestCompleted(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        return quest != null && quest.objectives.TrueForAll(o => o.IsCompleted);
    }

    public void HandInQuest(string questID)
    {
        QuestProgress questProgress = activeQuests.Find(q => q.QuestID == questID);
        if (questProgress == null) return;

        Quest quest = questProgress.quest;
        if (quest.consumeRequiredItemsOnHandIn)
        {
            if (!RemoveRequiredItemsFromInventory(questID))
            {
                return;
            }
        }
        else
        {
            if (!HasRequiredItemsForQuest(questID))
            {
                return;
            }
        }
        
        handinQuestIDs.Add(questID);
        activeQuests.Remove(questProgress);
        UpdateUI();
    }

    public bool IsQuestHandedIn(string questID)
    {
        return handinQuestIDs.Contains(questID);
    }

    public bool HasRequiredItemsForQuest(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if (quest == null) return false;

        Dictionary<int, int> requiredItems = new();

        foreach (QuestObjective objective in quest.objectives)
        {
            if (objective.type == ObjectiveType.CollectItem &&
                int.TryParse(objective.objectiveID, out int itemID))
            {
                requiredItems[itemID] = objective.requiredAmount;
            }
        }

        Dictionary<int, int> itemCounts = InventoryController.Instance.GetItemCounts();

        foreach (var item in requiredItems)
        {
            if (itemCounts.GetValueOrDefault(item.Key) < item.Value)
            {
                return false;
            }
        }

        return true;
    }

    public void NotifyLocationReached(string locationId)
    {
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            var quest = activeQuests[i];
            if (quest.IsCompleted)
                continue;

            quest.TryCompleteReachLocation(locationId);
        }
    }

    public bool RemoveRequiredItemsFromInventory(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if (quest == null) return false;

        if (!HasRequiredItemsForQuest(questID)) return false;

        Dictionary<int, int> requiredItems = new();

        foreach (QuestObjective objective in quest.objectives)
        {
            if (objective.type == ObjectiveType.CollectItem && int.TryParse(objective.objectiveID, out int itemID))
            {
                requiredItems[itemID] = objective.requiredAmount;
            }
        }

        foreach (var itemRequirement in requiredItems)
        {
            InventoryController.Instance.RemoveItemsFromInventory(itemRequirement.Key, itemRequirement.Value);
        }

        return true;
    }
    
    public void LoadQuestProgress(List<QuestProgress> savedQuests)
    {
        activeQuests = savedQuests ?? new();

        CheckInventoryForQuests();
        UpdateUI();
    }

    public void UpdateUI()
    {
        foreach(Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        foreach(var quest in activeQuests)
        {
            if (quest.quest == null) continue;
            
            List<string> lines = new();

            foreach (var objective in quest.objectives)
            {
                lines.Add($"{objective.description} ({objective.currentAmount}/{objective.requiredAmount})"); //Collect 5 Potions (0/5)
            }

            EntryBlockUI entry = Instantiate(entryPrefab, listContent);
            entry.Setup(quest.quest.questName, lines);
        }
    }

}
