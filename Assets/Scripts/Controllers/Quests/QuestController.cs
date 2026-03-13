using System.Collections.Generic;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }
    public List<QuestProgress> activeQuests = new();
    private QuestUI questUI;
    public List<string> handinQuestIDs = new();
    [SerializeField] Sprite questIcon;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        questUI = FindFirstObjectByType<QuestUI>();
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
        questUI.UpdateQuestUI();
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
        questUI.UpdateQuestUI();
    }

    public bool IsQuestCompleted(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        return quest != null && quest.objectives.TrueForAll(o => o.IsCompleted);
    }

    public void HandInQuest(string questID)
    {
        //Try remove required items
        if (!RemoveRequiredItemsFromInventory(questID))
        {
            //Quest couldn't be completed - missing items
            return;
        }

        //Remove quest from quest log
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if(quest != null)
        {
            handinQuestIDs.Add(questID);
            activeQuests.Remove(quest);
            questUI.UpdateQuestUI();
        }
    }

    public bool IsQuestHandedIn(string questID)
    {
        return handinQuestIDs.Contains(questID);
    }

    public bool RemoveRequiredItemsFromInventory(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if (quest == null) return false;

        Dictionary<int, int> requiredItems = new();

        //Item requirements from objectives
        foreach(QuestObjective objective in quest.objectives)
        {
            if(objective.type == ObjectiveType.CollectItem && int.TryParse(objective.objectiveID, out int itemID))
            {
                requiredItems[itemID] = objective.requiredAmount;
            }
        }

        //Verify we have items
        Dictionary<int, int> itemCounts = InventoryController.Instance.GetItemCounts();
        foreach(var item in requiredItems)
        {
            if(itemCounts.GetValueOrDefault(item.Key) < item.Value)
            {
                //Not enough items to complete quest
                return false;
            }
        }

        //Remove required items from inventory
        foreach(var itemRequirement in requiredItems)
        {
            InventoryController.Instance.RemoveItemsFromInventory(itemRequirement.Key, itemRequirement.Value);
        }

        return true;
    }
    
    public void LoadQuestProgress(List<QuestProgress> savedQuests)
    {
        activeQuests = savedQuests ?? new();

        CheckInventoryForQuests();
        questUI.UpdateQuestUI();
    }

}
