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

    private bool isOpen;

    private void Awake()
    {
        Instance = this;

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
        if (!isOpen)
            return;

        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Keyboard.current.eKey.wasPressedThisFrame))
        {
            Hide();
        }
    }

    public void Show(Sprite image, string symbolTitle, string description, System.Action letterAction = null)
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

        if (readLetterButton != null)
        {
            readLetterButton.gameObject.SetActive(letterAction != null);
            readLetterButton.onClick.RemoveAllListeners();

            if (letterAction != null)
                readLetterButton.onClick.AddListener(OnReadLetterClicked);
        }

        panel.SetActive(true);
        isOpen = true;
        Time.timeScale = 0f;
    }

    private void OnReadLetterClicked()
    {
        var action = currentLetterAction;
        currentLetterAction = null;

        Hide();

        Time.timeScale = 1f;
        action?.Invoke();
    }

    public void Hide()
    {
        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        if (readLetterButton != null)
            readLetterButton.gameObject.SetActive(false);

        Time.timeScale = 1f;
        currentLetterAction = null;
    }
}