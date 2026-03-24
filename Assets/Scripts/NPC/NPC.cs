using System.Collections;
using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue[] dialogueStages;
    private NPCDialogue dialogueData;
    private int currentStageIndex = 0;
    private DialogueController dialogueUI;
    private int dialogueIndex;
    private Coroutine typingCoroutine;
    private Coroutine autoProgressCoroutine;
    private bool isTyping, isDialogueActive;
    private float nextVoiceTime;
    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState questState = QuestState.NotStarted;

    private void Start()
    {
        dialogueUI = DialogueController.Instance;
        ResolveCurrentDialogueData();
        SyncQuestState();
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

    private void ResolveCurrentDialogueData()
    {
        if (dialogueStages == null || dialogueStages.Length == 0)
            return;

        currentStageIndex = 0;
        dialogueData = dialogueStages[0];

        for (int i = 0; i < dialogueStages.Length; i++)
        {
            NPCDialogue stage = dialogueStages[i];
            if (stage == null || stage.quest == null)
            {
                currentStageIndex = i;
                dialogueData = stage;
                return;
            }

            string questId = stage.quest.questID;

            if (QuestController.Instance.IsQuestHandedIn(questId))
            {
                if (i + 1 < dialogueStages.Length)
                {
                    currentStageIndex = i + 1;
                    dialogueData = dialogueStages[i + 1];
                }
                else
                {
                    currentStageIndex = i;
                    dialogueData = stage;
                }
            }
            else
            {
                currentStageIndex = i;
                dialogueData = stage;
                return;
            }
        }
    }

    void StartDialogue()
    {
        dialogueUI.SetActiveNPC(this);

        SyncQuestState();

        //Set dialogue line based on questState
        if (questState == QuestState.NotStarted)
        {
            dialogueIndex = 0;
        }
        else if (questState == QuestState.InProgress)
        {
            if (IsQuestReadyToHandIn())
            {
                if (TryHandInQuest())
                {
                    dialogueIndex = dialogueData.questCompletedIndex;
                }
                else
                {
                    dialogueIndex = dialogueData.questInProgressIndex;
                }
            }
            else
            {
                dialogueIndex = dialogueData.questInProgressIndex;
            }
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

        if (QuestController.Instance.IsQuestHandedIn(questID))
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
        if (!isDialogueActive)
            yield break;

        isTyping = true;
        dialogueUI.SetDialogueText("");

        foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
        {
            if (!isDialogueActive)
                yield break;

            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter);
            if (!char.IsWhiteSpace(letter) && Time.unscaledTime >= nextVoiceTime)
            {
                AudioClip clip = dialogueData.voiceSounds[
                    Random.Range(0, dialogueData.voiceSounds.Length)
                ];
                SoundEffectManager.PlayVoice(
                    new AudioClipData(clip, dialogueData.voiceVolume),
                    dialogueData.voicePitch
                );

                nextVoiceTime = Time.unscaledTime + Random.Range(0.04f, 0.08f);
            }
            yield return new WaitForSecondsRealtime(dialogueData.typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;

        if (!isDialogueActive)
            yield break;

        HandleJournalUpdateForCurrentLine();

        if (HandleQuestHandInJump())
            yield break;

        // Auto progress
        if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
        {
            autoProgressCoroutine = StartCoroutine(AutoProgress());
        }
    }

    void NextLine()
    {
        if (!isDialogueActive)
            return;

        SoundEffectManager.StopVoice();

        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            dialogueUI.SetDialogueText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;

            if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex] && isDialogueActive)
            {
                if (autoProgressCoroutine != null)
                    StopCoroutine(autoProgressCoroutine);

                autoProgressCoroutine = StartCoroutine(AutoProgress());
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

        if (dialogueIndex + 1 < dialogueData.dialogueLines.Length)
        {
            dialogueIndex++;
            DisplayCurrentLine();
        }
        else
        {
            if (currentStageIndex + 1 < dialogueStages.Length)
            {
                currentStageIndex++;
                dialogueData = dialogueStages[currentStageIndex];
                dialogueIndex = 0;
                questState = QuestState.NotStarted;
                DisplayCurrentLine();
            }
            else
            {
                EndDialogue();
            }
        }
    }

    bool TryHandInQuest()
    {
        if (dialogueData.quest == null) return false;

        string id = dialogueData.quest.questID;

        if (!QuestController.Instance.IsQuestCompleted(id)) return false;
        if (QuestController.Instance.IsQuestHandedIn(id)) return false;

        RewardsController.Instance.GiveQuestReward(dialogueData.quest, gameObject);
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

            if (dialogueIndex >= 0 && dialogueIndex < dialogueData.dialogueLines.Length)
            {
                DisplayCurrentLine();
            }
            else
            {
                EndDialogue();
            }
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

            dialogueUI.CreateChoiceButton(
                choice.choices[i],
                () => ChooseOption(nextIndex, givesQuest)
            );
        }
        dialogueUI.FitChoiceButtonsWidth();
        dialogueUI.AdjustDialoguePanelPosition(choice.choices.Length);
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
        if (!isDialogueActive)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (autoProgressCoroutine != null)
        {
            StopCoroutine(autoProgressCoroutine);
            autoProgressCoroutine = null;
        }
        nextVoiceTime = Time.unscaledTime;
        typingCoroutine = StartCoroutine(TypeLine());
    }

    IEnumerator AutoProgress()
    {
        yield return new WaitForSecondsRealtime(dialogueData.autoProgressDelay);

        autoProgressCoroutine = null;

        if (!isDialogueActive)
            yield break;

        NextLine();
    }
    public void EndDialogue()
    {
        isDialogueActive = false;
        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (autoProgressCoroutine != null)
        {
            StopCoroutine(autoProgressCoroutine);
            autoProgressCoroutine = null;
        }

        SoundEffectManager.StopVoice();
        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogueUI(false);
        dialogueUI.SetActiveNPC(null);
        PauseController.SetPause(false);
    }

    private bool IsQuestReadyToHandIn()
    {
        if (dialogueData.quest == null) return false;

        string questID = dialogueData.quest.questID;
        return QuestController.Instance.IsQuestCompleted(questID)
            && !QuestController.Instance.IsQuestHandedIn(questID);
    }

    void HandleJournalUpdateForCurrentLine()
    {
        if (dialogueData.journalUpdates == null) return;
        if (dialogueIndex < 0 || dialogueIndex >= dialogueData.journalUpdates.Length) return;

        DialogueJournal update = dialogueData.journalUpdates[dialogueIndex];
        if (update == null || !update.trigger) return;

        if (!string.IsNullOrEmpty(update.topicId))
        {
            JournalController.Instance.UnlockOrUpdateTopic(update.topicId, update.stageIndex);
        }
    }
}
