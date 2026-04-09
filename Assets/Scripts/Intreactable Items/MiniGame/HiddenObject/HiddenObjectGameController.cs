using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HiddenObjectGameController : MonoBehaviour, IMiniGame
{
    [SerializeField] Transform targetsContainer;
    [SerializeField] HiddenObjectTargetUI targetSlotPrefab;
    private readonly Dictionary<HiddenObjectItem, HiddenObjectTargetUI> slotsByItem = new();
    private MiniGameItem currentSource;
    private List<HiddenObjectItem> targetItems = new();
    private int foundCount;
    private bool isCompleted;
    private bool isCompleting = false;
    private bool isOpen;
    public event Action OnCompleted;


    private void Awake()
    {
        targetItems = GetComponentsInChildren<HiddenObjectItem>(true).ToList();
    }

    public void Open(MiniGameItem source = null)
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
        isCompleted = false;
        foundCount = 0;

        foreach (var item in targetItems)
        {
            item.Init(this);
        }
        
        BuildTargetsList(targetItems);
    }

    public void BuildTargetsList(List<HiddenObjectItem> targetItems)
    {
        ClearTargets();
        foreach (var item in targetItems)
        {
            HiddenObjectTargetUI slot = Instantiate(targetSlotPrefab, targetsContainer);
            slot.Setup(item.DisplayName, item.Icon);
            slotsByItem[item] = slot;
        }
    }

    private void ClearTargets()
    {
        slotsByItem.Clear();
        for (int i = targetsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(targetsContainer.GetChild(i).gameObject);
        }
    }

    public void MarkFound(HiddenObjectItem item)
    {
        if (slotsByItem.TryGetValue(item, out var slot))
        {
            slot.SetFound(true);
            SoundEffectManager.Play("CollectItem");
        }
    }
    public void TrySelectItem(HiddenObjectItem item)
    {
        if (isCompleted || item == null) return;
        if (item.IsFound) return;

        item.MarkAsFound();
        foundCount++;

        MarkFound(item);

        if (foundCount >= targetItems.Count && !isCompleting)
        {
            isCompleting = true;
            StartCoroutine(CompleteWithDelay());
        }
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
        OnCompleted?.Invoke();
        Close();
    }   
}