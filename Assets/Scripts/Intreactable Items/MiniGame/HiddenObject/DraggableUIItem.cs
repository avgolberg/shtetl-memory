using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform dragRect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private RectTransform parentRect;
    private Vector2 dragOffset;

    private void Awake()
    {
        dragRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        parentRect = dragRect.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;
        }

        transform.SetAsLastSibling();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint))
        {
            dragOffset = dragRect.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRect == null || canvas == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint))
        {
            Vector2 targetPos = localPoint + dragOffset;
            dragRect.anchoredPosition = ClampToParent(targetPos);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
    
    private Vector2 ClampToParent(Vector2 targetPos)
    {
        Vector2 parentSize = parentRect.rect.size;

        float itemWidth = dragRect.rect.width * dragRect.localScale.x;
        float itemHeight = dragRect.rect.height * dragRect.localScale.y;

        float halfItemWidth = itemWidth * 0.5f;
        float halfItemHeight = itemHeight * 0.5f;

        float minX = -parentSize.x * 0.5f + halfItemWidth;
        float maxX =  parentSize.x * 0.5f - halfItemWidth;

        float minY = -parentSize.y * 0.5f + halfItemHeight;
        float maxY =  parentSize.y * 0.5f - halfItemHeight;

        return new Vector2(
            Mathf.Clamp(targetPos.x, minX, maxX),
            Mathf.Clamp(targetPos.y, minY, maxY)
        );
    }
}