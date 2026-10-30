using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; } //Singleton Instance
    public NPC ActiveNPC { get; private set; }
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;
    [SerializeField] private float minButtonWidth = 220f;
    [SerializeField] private float maxButtonWidth = 650f;
    [SerializeField] private float horizontalPadding = 40f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject); //Make sure only one instance
    }

    //Space shows the whole line, or the next line (one step per press). F8 is a development-only skip.
    void Update()
    {
        if (ActiveNPC == null || Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            ActiveNPC.Advance();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else if (Keyboard.current.f8Key.wasPressedThisFrame)
            ActiveNPC.DevSkipToChoiceOrEnd();
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    //Development only: not compiled into release builds
    void OnGUI()
    {
        if (ActiveNPC == null) return;
        if (GUI.Button(new Rect(Screen.width - 250, 10, 240, 32), "DEV: Skip dialogue (F8)"))
            ActiveNPC.DevSkipToChoiceOrEnd();
    }
#endif

    public void SetActiveNPC(NPC npc)
    {
        ActiveNPC = npc;
    }

    public void ShowDialogueUI(bool show)
    {
        dialoguePanel.SetActive(show); //Toggle UI visability

        //A button left selected (e.g. the close button) would also react to Space
        if (show && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void SetNPCInfo(string npcName, Sprite portrait)
    {
        nameText.text = npcName;
        portraitImage.sprite = portrait;
    }

    //The whole line is set at once, so wrapping and auto-size are final from the first letter;
    //a typed line starts hidden and is revealed with SetVisibleCharacters
    public void SetDialogueText(string text, bool typed = false)
    {
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = typed ? 0 : int.MaxValue;
    }

    public void SetVisibleCharacters(int count)
    {
        dialogueText.maxVisibleCharacters = count;
    }

    public void ClearChoices()
    {
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);
        AdjustDialoguePanelPosition(0);
    }

    public GameObject CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
        choiceButton.GetComponentInChildren<TMP_Text>().text = choiceText;
        choiceButton.GetComponent<Button>().onClick.AddListener(onClick);
        return choiceButton;
    }

    public void FitChoiceButtonsWidth()
    {
        float maxPreferredWidth = 0f;
        for (int i = 0; i < choiceContainer.childCount; i++)
        {
            Transform child = choiceContainer.GetChild(i);
            TMP_Text text = child.GetComponentInChildren<TMP_Text>();
            if (text == null) continue;

            text.ForceMeshUpdate();

            float preferredWidth = text.GetPreferredValues(text.text, Mathf.Infinity, Mathf.Infinity).x;

            if (preferredWidth > maxPreferredWidth)
                maxPreferredWidth = preferredWidth;
        }

        float finalWidth = Mathf.Clamp(
            maxPreferredWidth + horizontalPadding,
            minButtonWidth,
            maxButtonWidth
        );

        for (int i = 0; i < choiceContainer.childCount; i++)
        {
            GameObject buttonObj = choiceContainer.GetChild(i).gameObject;

            LayoutElement layout = buttonObj.GetComponent<LayoutElement>();
            if (layout == null)
                layout = buttonObj.AddComponent<LayoutElement>();

            layout.preferredWidth = finalWidth;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(choiceContainer as RectTransform);
    }

    public void AdjustDialoguePanelPosition(int choiceCount)
    {
        float y = choiceCount switch
        {
            0 => 50f,
            1 or 2 => 75f,
            _ => 115f
        };
        StartCoroutine(MovePanel(y));
    }
    
    IEnumerator MovePanel(float targetY)
    {
        float duration = 0.15f;
        float time = 0f;

        RectTransform rt = dialoguePanel.GetComponent<RectTransform>();

        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, targetY);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        rt.anchoredPosition = targetPos;
    }
    public void CloseDialogue()
    {
        ActiveNPC?.EndDialogue();
    }    
}