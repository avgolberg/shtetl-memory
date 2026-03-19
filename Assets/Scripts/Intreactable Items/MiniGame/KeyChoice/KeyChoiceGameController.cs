using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyChoiceGameController : MonoBehaviour, IMiniGame
{
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private KeyChoiceButton buttonPrefab;
    [SerializeField] private TMP_Text feedbackText;
    private int correctKeyId;
   
    private MiniGameItem currentSource;
   
    private readonly List<KeyChoiceButton> spawnedButtons = new();
    private List<Item> availableKeys = new();

    private bool isOpen, isCompleted;

    public void Open(MiniGameItem source)
    {
        if (isOpen) return;

        isOpen = true;
        currentSource = source;
        gameObject.SetActive(true);
        PauseController.SetPause(true);
        InitializeGame();
    }

    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
        PauseController.SetPause(false);
        currentSource = null;
    }

    public void InitializeGame()
    {
        BuildKeyList();
        isCompleted = false;
        if (availableKeys.Count == 0)
        {
            SetFeedback("No keys available.");
            Close();
            return;
        }

        correctKeyId = Random.Range(1, availableKeys.Count);
        SetFeedback("Which key fits the lock?");
        BuildButtons();
    }

    private void BuildKeyList()
    {
        availableKeys.Clear();
        availableKeys = InventoryController.Instance.GetItemsByType(ItemType.Key);
    }

    private void BuildButtons()
    {
        foreach (Item key in availableKeys)
        {
            KeyChoiceButton button = Instantiate(buttonPrefab, buttonsContainer);
            spawnedButtons.Add(button);
            button.Setup(key, spawnedButtons.Count, this);
        }
    }

    public void TryKey(KeyChoiceButton clickedButton, int keyId)
    {
        if (!isOpen || isCompleted || clickedButton == null) return;

        if (keyId == correctKeyId)
        {
            isCompleted = true;
            
            foreach (var button in spawnedButtons)
                button.MarkWrong();

            clickedButton.MarkCorrect();

            SetFeedback("The key fits!");
            SoundEffectManager.Play("CollectItem");
            StartCoroutine(CompleteWithDelay());
            return;
        }

        clickedButton.MarkWrong();
        SetFeedback("This key doesn't fit.");
        SoundEffectManager.Play("NegativeSound");
    }
   
    private IEnumerator CompleteWithDelay()
    {
        yield return new WaitForSeconds(1.5f);
        SoundEffectManager.Play("MiniGameCompleted");
        CompleteGame();
    }

    private void CompleteGame()
    {
        currentSource?.CompleteMiniGame();
        Close();
    }   
    
    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}