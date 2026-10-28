using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TracePoint : MonoBehaviour, IPointerEnterHandler
{
    private Image image;
    private bool completed;

    public bool Completed => completed;
    public RectTransform RectTransform { get; private set; }

    private readonly Color futureColor = new Color(1f, 1f, 1f, 0.75f);
    private readonly Color currentColor = new Color(1f, 0.15f, 0.1f, 1f);

    private void Awake()
    {
        image = GetComponent<Image>();
        RectTransform = GetComponent<RectTransform>();
        image.raycastTarget = true;
    }

    public void SetFuture()
    {
        if (completed || image == null)
            return;

        image.enabled = true;
        image.color = futureColor;
    }

    public void SetCurrent()
    {
        if (completed || image == null)
            return;

        image.enabled = true;
        image.color = currentColor;
    }

    public void ResetPoint()
    {
        completed = false;

        if (image != null)
        {
            image.enabled = true;
            image.color = futureColor;
        }
    }

    public void Complete()
    {
        if (completed)
            return;

        completed = true;

        if (image != null)
            image.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        VytynankaMiniGame.Instance?.TryCompletePoint(this);
    }

    public void Hide()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
            image.enabled = false;
    }
}