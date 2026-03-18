using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HiddenObjectTargetUI : MonoBehaviour
{
    private Image iconImage;
    private TMP_Text nameText;
    private GameObject checkmark;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        iconImage = transform.Find("Image")?.GetComponent<Image>();
        nameText = GetComponentInChildren<TMP_Text>(true);
        checkmark = transform.Find("Checkmark")?.gameObject;
    }

    public void Setup(string displayName, Sprite icon)
    {
        nameText.text = displayName;
        iconImage.sprite = icon;
        SetFound(false);
    }

    public void SetFound(bool isFound)
    {
        checkmark.SetActive(isFound);
        canvasGroup.alpha = isFound ? 0.5f : 1f;
        nameText.fontStyle = isFound ? FontStyles.Strikethrough : FontStyles.Normal;
    }
}


