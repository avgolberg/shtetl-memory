using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    public string npcName;
    public Sprite npcPortrait;

    [TextArea(2, 5)]
    public string[] dialogueLines;
    
    public bool[] autoProgressLines;
    public bool[] endDialogueLines; //Mark where dialogue ends

    public float autoProgressDelay = 1.5f;

    public float typingSpeed = 0.05f;

    public AudioClip voiceSound;
    public float voicePitch = 1f;
    public DialogueChoice[] choices;
}

[System.Serializable]
public class DialogueChoice
{
    public int dialogueIndex; //Dialogue line where choices appear
    public string[] choices; //Player response options
    public int[] nextDialogueIndexes; //Where choice leads
}