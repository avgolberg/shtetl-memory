using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    public string npcName;
    public Sprite npcPortrait;

    public string[] dialogueLines;

    public bool[] autoProgressLines;
    public bool[] endDialogueLines; //Mark where dialogue ends
    public bool[] questHandInIndex;
    public DialogueJournal[] journalUpdates;
    public float autoProgressDelay = 1.5f;

    public float typingSpeed = 0.05f;

    public AudioClip[] voiceSounds;
    public float voiceVolume = 1f;
    public float voicePitch = 1f;
    public DialogueChoice[] choices;
    public Quest quest; //Quest NPC gives
    public int questInProgressIndex; //What does he say while quest in progress
    public int questCompletedIndex; //What does he say when quest completed
}

[System.Serializable]
public class DialogueChoice
{
    public int dialogueIndex; //Dialogue line where choices appear
    public string[] choices; //Player response options
    public int[] nextDialogueIndexes; //Where choice leads
    public bool[] givesQuest; //If choise gives quest
}

[System.Serializable]
public class DialogueJournal
{
    public bool trigger;
    public string topicId;
    public int stageIndex;
}