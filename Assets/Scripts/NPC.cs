using System.Collections;
using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue dialogueData;
    private DialogueController dialogueUI;
    private int dialogueIndex;
    private bool isTyping, isDialogueActive;
    private float nextVoiceTime;
    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState questState = QuestState.NotStarted;

    private void Start()
    {
        dialogueUI = DialogueController.Instance;
        dialogueUI.SetActiveNPC(this);
    }

    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        if (dialogueData == null || (PauseController.IsGamePaused && !isDialogueActive))
            return;

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        SyncQuestState();

        //Set dialogue line based on questState
        if (questState == QuestState.NotStarted)
        {
            dialogueIndex = 0;
        }
        else if (questState == QuestState.InProgress)
        {
            dialogueIndex = dialogueData.questInProgressIndex;
        }
        else if (questState == QuestState.Completed)
        {
            dialogueIndex = dialogueData.questCompletedIndex;
        }

        isDialogueActive = true;

        dialogueUI.SetNPCInfo(dialogueData.npcName, dialogueData.npcPortrait);
        dialogueUI.ShowDialogueUI(true);
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    private void SyncQuestState()
    {
        if (dialogueData.quest == null) return;

        string questID = dialogueData.quest.questID;

        if (QuestController.Instance.IsQuestCompleted(questID) || QuestController.Instance.IsQuestHandedIn(questID))
        {
            questState = QuestState.Completed;
        }
        else if (QuestController.Instance.IsQuestActive(questID))
        {
            questState = QuestState.InProgress;
        }
        else
        {
            questState = QuestState.NotStarted;
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");

        foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter);
            if (!char.IsWhiteSpace(letter) && Time.unscaledTime >= nextVoiceTime)
            {
                SoundEffectManager.PlayVoice(
                    new AudioClipData(dialogueData.voiceSound, dialogueData.voiceVolume),
                    dialogueData.voicePitch
                );

                nextVoiceTime = Time.unscaledTime + 0.05f;
            }
            yield return new WaitForSecondsRealtime(dialogueData.typingSpeed);
        }

        isTyping = false;

        if (HandleQuestHandInJump())
            yield break;

        // Auto progress
        if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
        {
            StartCoroutine(AutoProgress());
        }
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines(); // Skip typing animation
            dialogueUI.SetDialogueText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;

            if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
            {
                StartCoroutine(AutoProgress());
            }
            return;
        }

        dialogueUI.ClearChoices();

        if (HandleQuestHandInJump())
            return;

        if (dialogueData.endDialogueLines.Length > dialogueIndex && dialogueData.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        foreach (DialogueChoice dialogueChoice in dialogueData.choices)
        {
            if (dialogueChoice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(dialogueChoice);
                return;
            }
        }

        if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    bool TryHandInQuest()
    {
        if (dialogueData.quest == null) return false;

        string id = dialogueData.quest.questID;

        if (!QuestController.Instance.IsQuestCompleted(id)) return false;
        if (QuestController.Instance.IsQuestHandedIn(id)) return false;

        RewardsController.Instance.GiveQuestReward(dialogueData.quest);
        QuestController.Instance.HandInQuest(id);
        questState = QuestState.Completed;
        
        return true;
    }

    bool HandleQuestHandInJump()
    {
        if (dialogueData.quest == null) return false;

        if (dialogueData.questHandInIndex == null) return false;
        if (dialogueIndex < 0 || dialogueIndex >= dialogueData.questHandInIndex.Length) return false;

        if (!dialogueData.questHandInIndex[dialogueIndex]) return false;

        if (TryHandInQuest())
        {
            dialogueIndex = dialogueData.questCompletedIndex;
            dialogueUI.ClearChoices();
            DisplayCurrentLine();
            return true;
        }
        return false;
    }

    void DisplayChoices(DialogueChoice choice)
    {
        for (int i = 0; i < choice.choices.Length; i++)
        {
            int nextIndex = choice.nextDialogueIndexes[i];
            bool givesQuest = choice.givesQuest[i];
            dialogueUI.CreateChoiceButton(choice.choices[i], () => ChooseOption(nextIndex, givesQuest));
        }
    }

    void ChooseOption(int nextIndex, bool givesQuest)
    {
        if (givesQuest)
        {
            QuestController.Instance.AcceptQuest(dialogueData.quest);
            questState = QuestState.InProgress;
        }

        dialogueIndex = nextIndex;
        dialogueUI.ClearChoices();
        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        nextVoiceTime = Time.unscaledTime;
        StartCoroutine(TypeLine());
    }

    IEnumerator AutoProgress()
    {
        yield return new WaitForSecondsRealtime(dialogueData.autoProgressDelay);
        NextLine();
    }
    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogueUI(false);
        PauseController.SetPause(false);
    }
}
