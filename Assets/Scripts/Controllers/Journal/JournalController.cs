using System.Collections.Generic;
using UnityEngine;

public class JournalController : MonoBehaviour
{
    public static JournalController Instance { get; private set; }
    [SerializeField] private List<JournalTopicData> allTopics;
    [SerializeField] Sprite journalIcon;
    [SerializeField] private Transform listContent;
    [SerializeField] private EntryBlockUI entryPrefab;

    private readonly Dictionary<string, JournalTopicRuntime> unlockedTopics = new();

    private void Awake()
    {
        Instance = this;
    }

    public void UnlockOrUpdateTopic(string topicId, int stageIndex)
    {
        JournalTopicData topicData = allTopics.Find(t => t.topicId == topicId);
        if (topicData == null) return;
        if (stageIndex < 0 || stageIndex >= topicData.stages.Count) return;

        if (!unlockedTopics.TryGetValue(topicId, out var runtime))
        {
            if (stageIndex != 0) return;

            JournalStageData stageData = topicData.stages[stageIndex];

            runtime = new JournalTopicRuntime
            {
                topicId = topicId,
                currentStage = stageIndex,
                currentTitle = stageData.title,
                lines = new List<string>(stageData.lines)
            };

            unlockedTopics.Add(topicId, runtime);
            ItemPickupUIController.Instance?.ShowItemPickup(stageData.title, journalIcon);
            UpdateUI();
            return;
        }

        if (stageIndex <= runtime.currentStage)
            return;

        if (stageIndex != runtime.currentStage + 1)
            return;

        JournalStageData newStage = topicData.stages[stageIndex];
        runtime.currentStage = stageIndex;
        runtime.currentTitle = newStage.title;

        foreach (string line in newStage.lines)
        {
            if (!runtime.lines.Contains(line))
                runtime.lines.Add(line);
        }

        ItemPickupUIController.Instance?.ShowItemPickup(newStage.title, journalIcon);
        UpdateUI();
    }

    public bool CanUnlockOrUpdateTopic(string topicId, int stageIndex)
    {
        JournalTopicData topicData = allTopics.Find(t => t.topicId == topicId);
        if (topicData == null) return false;
        if (stageIndex < 0 || stageIndex >= topicData.stages.Count) return false;

        if (!unlockedTopics.TryGetValue(topicId, out var runtime))
        {
            return stageIndex == 0;
        }

        if (stageIndex <= runtime.currentStage)
            return false;

        return stageIndex == runtime.currentStage + 1;
    }

    public void UpdateUI()
    {
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var pair in unlockedTopics)
        {
            JournalTopicRuntime topic = pair.Value;
            EntryBlockUI entry = Instantiate(entryPrefab, listContent);
            entry.Setup(topic.currentTitle, topic.lines);
        }
    }

    public List<JournalTopicSaveData> GetSaveData()
    {
        List<JournalTopicSaveData> result = new();

        foreach (var pair in unlockedTopics)
        {
            JournalTopicRuntime topic = pair.Value;

            result.Add(new JournalTopicSaveData
            {
                topicId = topic.topicId,
                unlockedStageIndex = topic.currentStage
            });
        }
        return result;
    }
    
    public void LoadFromSave(List<JournalTopicSaveData> saveData)
    {
        unlockedTopics.Clear();

        if (saveData == null)
        {
            UpdateUI();
            return;
        }

        foreach (var entry in saveData)
        {
            JournalTopicData topicData = allTopics.Find(t => t.topicId == entry.topicId);
            if (topicData == null) continue;

            int stageIndex = Mathf.Clamp(entry.unlockedStageIndex, 0, topicData.stages.Count - 1);

            List<string> combinedLines = new();
            for (int i = 0; i <= stageIndex; i++)
            {
                combinedLines.AddRange(topicData.stages[i].lines);
            }

            JournalTopicRuntime runtime = new JournalTopicRuntime
            {
                topicId = entry.topicId,
                currentStage = stageIndex,
                currentTitle = topicData.stages[stageIndex].title,
                lines = combinedLines
            };

            unlockedTopics[entry.topicId] = runtime;
        }

        UpdateUI();
    }
}