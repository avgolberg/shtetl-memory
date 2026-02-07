using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPC : MonoBehaviour, IInteractable
{
    [Header("Dialogue Data")]
    public NPCDialogue dialogueData;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public Image portraitImage;

    private int dialogueIndex;
    private bool isTyping;
    private bool isDialogueActive;

    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        if (dialogueData == null)
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
        isDialogueActive = true;
        dialogueIndex = 0;

        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcPortrait;

        dialoguePanel.SetActive(true);
        PauseController.SetPause(true);

        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");

        string line = dialogueData.dialogueLines[dialogueIndex];

        foreach (char letter in line)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;

        // Auto progress
        if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
        {
            StartCoroutine(AutoProgress());
        }
    }

    void NextLine()
    {
        // Skip typing animation
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            
            if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
            {
                StartCoroutine(AutoProgress());
            }
            return;
        }

        dialogueIndex++;

        if (dialogueIndex < dialogueData.dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }
    IEnumerator AutoProgress()
    {
        yield return new WaitForSeconds(dialogueData.autoProgressDelay);
        NextLine();
    }
    public void EndDialogue()
    {
        StopAllCoroutines();

        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);

        PauseController.SetPause(false);
    }
}
