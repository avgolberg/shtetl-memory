using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GraveInfoUI : MonoBehaviour
{
    public static GraveInfoUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private Image graveImage;
    [SerializeField] private TMP_Text symbolTitleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button readLetterButton;

    private System.Action currentLetterAction;
    private System.Action currentClosedAction; //Called when the player closes the popup instead of pressing the action button
    private TMP_Text actionButtonText;
    private string defaultActionLabel;
    private bool keyRunsAction;
    private int openedFrame;

    private bool isOpen;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;

        if (readLetterButton != null)
        {
            actionButtonText = readLetterButton.GetComponentInChildren<TMP_Text>(true);
            if (actionButtonText != null)
                defaultActionLabel = actionButtonText.text;
        }

        if (panel != null)
            panel.SetActive(false);

        if (readLetterButton != null)
            readLetterButton.gameObject.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void Update()
    {
        //Skip the frame the popup opened in: the key press that opened it must not close it
        if (!isOpen || Time.frameCount == openedFrame)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame && keyRunsAction && currentLetterAction != null)
        {
            OnReadLetterClicked();
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                 Keyboard.current.eKey.wasPressedThisFrame)
        {
            Hide();
        }
    }

    //actionLabel: text for the action button (e.g. "Next"); when set, E also runs the action instead of closing.
    //closedAction: runs when the player closes the popup without using the action button.
    public void Show(Sprite image, string symbolTitle, string description, System.Action letterAction = null,
                     string actionLabel = null, System.Action closedAction = null)
    {
        if (panel == null)
        {
            Debug.LogError("[GraveInfoUI] Panel is missing.", this);
            return;
        }

        if (graveImage != null)
        {
            graveImage.sprite = image;
            graveImage.preserveAspect = true;
        }

        if (symbolTitleText != null)
            symbolTitleText.text = symbolTitle;

        if (descriptionText != null)
            descriptionText.text = description;

        currentLetterAction = letterAction;
        currentClosedAction = closedAction;
        keyRunsAction = !string.IsNullOrEmpty(actionLabel);

        if (actionButtonText != null)
            actionButtonText.text = keyRunsAction ? actionLabel : defaultActionLabel;

        if (readLetterButton != null)
        {
            readLetterButton.gameObject.SetActive(letterAction != null);
            readLetterButton.onClick.RemoveAllListeners();

            if (letterAction != null)
                readLetterButton.onClick.AddListener(OnReadLetterClicked);
        }

        panel.SetActive(true);
        isOpen = true;
        openedFrame = Time.frameCount;
        Time.timeScale = 0f;
    }

    private void OnReadLetterClicked()
    {
        var action = currentLetterAction;
        currentLetterAction = null;
        currentClosedAction = null;

        Hide();

        Time.timeScale = 1f;
        action?.Invoke();
    }

    public void Hide()
    {
        var closedAction = currentClosedAction;
        currentClosedAction = null;
        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        if (readLetterButton != null)
            readLetterButton.gameObject.SetActive(false);

        Time.timeScale = 1f;
        currentLetterAction = null;
        closedAction?.Invoke();
    }
}