using System.Collections.Generic;

[System.Serializable]
public class JournalTopicRuntime
{
    public string topicId;
    public int currentStage;
    public string currentTitle;
    public List<string> lines = new();
}