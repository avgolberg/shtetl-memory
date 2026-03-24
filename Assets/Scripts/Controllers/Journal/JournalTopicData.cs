using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JournalTopic", menuName = "Journal/Topic")]
public class JournalTopicData : ScriptableObject
{
    public string topicId;
    public List<JournalStageData> stages;
}