using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyChoiceButton : MonoBehaviour
{
    private Image iconImage;
    private TMP_Text labelText;
    private Button button;
    private CanvasGroup canvasGroup;

    private Item keyItem;
    private int keyId;
    private bool isUsed;
    private KeyChoiceGameController controller;
    private ItemDictionary itemDictionary;

    void Awake()
    {
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        iconImage = transform.Find("Image")?.GetComponent<Image>();
        labelText = GetComponentInChildren<TMP_Text>(true);
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(Item item, int keyId, KeyChoiceGameController gameController)
    {
        keyItem = item;
        this.keyId = keyId;
        controller = gameController;
        isUsed = false;

        if (iconImage != null)
            iconImage.sprite = itemDictionary.GetItemPrefab(item.itemId).GetComponent<Image>().sprite;

        if (labelText != null)
            labelText.text = item.Name;

        canvasGroup.alpha = 1f;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    public void MarkWrong()
    {
        isUsed = true;
        canvasGroup.alpha = 0.35f;
        canvasGroup.interactable = false;
    }

    public void MarkCorrect()
    {
        isUsed = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
    }

    private void OnClick()
    {
        if (isUsed || keyItem == null || controller == null) return;
        controller.TryKey(this, keyId);
    }
}