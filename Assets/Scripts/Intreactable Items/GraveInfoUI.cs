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
    [SerializeField] private TMP_Text imageOverlayText; //Optional: text printed on the image (e.g. an invitation's wording)

    //The panel fits its content (top to bottom: image, title, description, buttons), in panel pixels
    const float ImageMaxWidth = 645f, ImageMaxHeight = 471f;
    const float TopPadding = 30f, ImageTitleGap = 16f, TitleDescriptionGap = 8f, DescriptionButtonsGap = 28f, BottomPadding = 43f;
    const float DescriptionMaxHeight = 235f; //Five lines

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
    //imageText: optional text printed on the image.
    public void Show(Sprite image, string symbolTitle, string description, System.Action letterAction = null,
                     string actionLabel = null, System.Action closedAction = null, string imageText = null)
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

        if (imageOverlayText != null)
        {
            imageOverlayText.text = imageText ?? "";
            imageOverlayText.gameObject.SetActive(!string.IsNullOrEmpty(imageText));
        }

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
        FitToContent(image);
        isOpen = true;
        openedFrame = Time.frameCount;
        Time.timeScale = 0f;
    }

    //Sizes the image to its sprite and the description to its text, then the panel (centred) around them,
    //so short pages don't leave large empty areas
    private void FitToContent(Sprite image)
    {
        if (graveImage == null || symbolTitleText == null || descriptionText == null || closeButton == null) return;

        float imageWidth = 0f, imageHeight = 0f;
        if (image != null)
        {
            float aspect = image.rect.width / image.rect.height;
            imageHeight = Mathf.Min(ImageMaxHeight, ImageMaxWidth / aspect);
            imageWidth = imageHeight * aspect;
        }
        var imageFrame = (RectTransform)graveImage.rectTransform.parent;
        imageFrame.gameObject.SetActive(image != null);
        imageFrame.sizeDelta = new Vector2(imageWidth, imageHeight);
        graveImage.rectTransform.sizeDelta = new Vector2(imageWidth, imageHeight);

        //Measured at the full height first, so auto-size keeps the size it would use there
        RectTransform description = descriptionText.rectTransform;
        description.sizeDelta = new Vector2(description.sizeDelta.x, DescriptionMaxHeight);
        descriptionText.ForceMeshUpdate();
        float descriptionHeight = string.IsNullOrEmpty(descriptionText.text)
            ? 0f
            : Mathf.Min(DescriptionMaxHeight, Mathf.Ceil(descriptionText.GetRenderedValues(true).y) + 8f);
        description.sizeDelta = new Vector2(description.sizeDelta.x, descriptionHeight);

        //Bottom-anchored rows, from the bottom up; the image hangs from the top
        var buttons = (RectTransform)closeButton.transform.parent;
        buttons.anchoredPosition = new Vector2(buttons.anchoredPosition.x, BottomPadding);
        float descriptionY = BottomPadding + buttons.sizeDelta.y + DescriptionButtonsGap;
        description.anchoredPosition = new Vector2(description.anchoredPosition.x, descriptionY);
        RectTransform title = symbolTitleText.rectTransform;
        float titleY = descriptionY + descriptionHeight + TitleDescriptionGap;
        title.anchoredPosition = new Vector2(title.anchoredPosition.x, titleY);
        imageFrame.anchoredPosition = new Vector2(imageFrame.anchoredPosition.x, -TopPadding);

        var panelRect = (RectTransform)panel.transform;
        float imageBlock = image != null ? imageHeight + ImageTitleGap : 0f;
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, titleY + title.sizeDelta.y + imageBlock + TopPadding);
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