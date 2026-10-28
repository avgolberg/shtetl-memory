using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VytynankaMiniGame : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool startDirectlyInTracing = true;
    public static VytynankaMiniGame Instance;
    private TracePath[] tracePaths;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject colorSelectPanel;
    [SerializeField] private GameObject workArea;
    [SerializeField] private Image paperImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button revealButton;

    [Header("Auto Find")]
    [SerializeField] private Transform tracePointsParent;
    [SerializeField] private RectTransform drawLineContainer;

    [Header("Cursor")]
    [SerializeField] private RectTransform scissorsCursorImage;
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, 35f);

    [Header("Drawing")]
    [SerializeField] private Image lineSegmentPrefab;
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private Color lineColor = new Color(0.25f, 0.18f, 0.12f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutPointSound;

    private readonly List<GameObject> drawnSegments = new();

    private bool isTracing;

    private void OnEnable()
    {
        AutoFindReferences();
        PrepareInitialState();
    }

    private void Awake()
    {
        Instance = this;
        AutoFindReferences();

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        if (workArea == null || !workArea.activeSelf)
            return;

        UpdateScissorsCursor();
    }

    private void PrepareInitialState()
    {
        ClearDrawnSegments();

        if (revealButton != null)
            revealButton.gameObject.SetActive(false);

        if (startDirectlyInTracing)
        {
            if (colorSelectPanel != null)
                colorSelectPanel.SetActive(false);

            if (workArea != null)
                workArea.SetActive(true);

            if (scissorsCursorImage != null)
                scissorsCursorImage.gameObject.SetActive(true);

            ResetGame();
        }
        else
        {
            if (colorSelectPanel != null)
                colorSelectPanel.SetActive(true);

            if (workArea != null)
                workArea.SetActive(false);

            if (scissorsCursorImage != null)
                scissorsCursorImage.gameObject.SetActive(false);

            HideAllPoints();
        }
    }

    private void ClearDrawnSegments()
    {
        foreach (var segment in drawnSegments)
        {
            if (segment != null)
                Destroy(segment);
        }

        drawnSegments.Clear();
    }

    private void AutoFindReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (colorSelectPanel == null)
            colorSelectPanel = transform.Find("Canvas/ColorSelectPanel")?.gameObject;

        if (workArea == null)
            workArea = transform.Find("Canvas/WorkArea")?.gameObject;

        if (paperImage == null)
            paperImage = transform.Find("Canvas/WorkArea/PaperImage")?.GetComponent<Image>();

        if (progressText == null)
            progressText = transform.Find("Canvas/WorkArea/ProgressText")?.GetComponent<TMP_Text>();

        if (revealButton == null)
            revealButton = transform.Find("Canvas/RevealButton")?.GetComponent<Button>();

        if (tracePointsParent == null)
            tracePointsParent = transform.Find("Canvas/WorkArea/TemplateGhostImage/TracePoints");

        if (drawLineContainer == null)
            drawLineContainer = transform.Find("Canvas/WorkArea/DrawLineContainer")?.GetComponent<RectTransform>();

        if (scissorsCursorImage == null)
            scissorsCursorImage = transform.Find("Canvas/WorkArea/ScissorsCursorImage")?.GetComponent<RectTransform>();


        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        tracePaths = tracePointsParent != null
         ? tracePointsParent.GetComponentsInChildren<TracePath>(true)
             .OrderBy(p => p.transform.GetSiblingIndex())
             .ToArray()
         : new TracePath[0];

        Debug.Log($"Found paths: {tracePaths.Length}");

        foreach (var path in tracePaths)
        {
            Debug.Log($"{path.name} : {path.TotalPoints} points");
        }
    }

    public void Open()
    {
        AutoFindReferences();

        panel.SetActive(true);
        Time.timeScale = 0f;

        colorSelectPanel.SetActive(true);
        workArea.SetActive(false);

        HideAllPoints();

        if (revealButton != null)
            revealButton.gameObject.SetActive(false);
    }

    public void SelectCream() => SelectPaperColor(new Color32(235, 220, 185, 255));
    public void SelectBlue() => SelectPaperColor(new Color32(150, 180, 210, 255));
    public void SelectRed() => SelectPaperColor(new Color32(180, 90, 80, 255));
    public void SelectGreen() => SelectPaperColor(new Color32(120, 160, 120, 255));

    private void SelectPaperColor(Color color)
    {
        if (paperImage != null)
            paperImage.color = color;

        colorSelectPanel.SetActive(false);
        workArea.SetActive(true);

        if (scissorsCursorImage != null)
            scissorsCursorImage.gameObject.SetActive(true);

        ResetGame();
    }

    private void ResetGame()
    {
        isTracing = false;

        foreach (var segment in drawnSegments)
            Destroy(segment);

        drawnSegments.Clear();

        foreach (var path in tracePaths)
            path.ResetPath();

        UpdateProgress();

        if (revealButton != null)
            revealButton.gameObject.SetActive(false);
    }

    public void TryCompletePoint(TracePoint point)
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            return;

        if (point == null || point.Completed)
            return;

        foreach (var path in tracePaths)
        {
            if (!path.ContainsPoint(point))
                continue;

            if (path.TryCompletePoint(point, out TracePoint previousPoint))
            {
                PlayCutSound();

                if (previousPoint != null)
                    DrawSegment(previousPoint.RectTransform, point.RectTransform);

                if (path.Completed && path.ClosePathToStart && path.FirstPoint != null)
                    DrawSegment(point.RectTransform, path.FirstPoint.RectTransform);

                UpdateProgress();

                if (AllPathsCompleted())
                    CompleteTracing();
            }

            return;
        }
    }

    private bool AllPathsCompleted()
    {
        if (tracePaths == null || tracePaths.Length == 0)
            return false;

        foreach (var path in tracePaths)
        {
            if (!path.Completed)
                return false;
        }

        return true;
    }

    private void DrawSegment(RectTransform from, RectTransform to)
    {
        if (lineSegmentPrefab == null || drawLineContainer == null)
            return;

        Image segment = Instantiate(lineSegmentPrefab, drawLineContainer);
        segment.color = lineColor;
        segment.raycastTarget = false;

        RectTransform segmentRect = segment.rectTransform;

        Vector2 start = GetLocalPoint(from, drawLineContainer);
        Vector2 end = GetLocalPoint(to, drawLineContainer);

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        segmentRect.anchorMin = new Vector2(0.5f, 0.5f);
        segmentRect.anchorMax = new Vector2(0.5f, 0.5f);
        segmentRect.pivot = new Vector2(0f, 0.5f);
        segmentRect.anchoredPosition = start;
        segmentRect.sizeDelta = new Vector2(distance, lineThickness);
        segmentRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        drawnSegments.Add(segment.gameObject);
    }

    private Vector2 GetLocalPoint(RectTransform source, RectTransform targetContainer)
    {
        Vector3 worldPos = source.TransformPoint(source.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetContainer,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private void UpdateScissorsCursor()
    {
        if (scissorsCursorImage == null || Mouse.current == null)
            return;

        RectTransform parent = scissorsCursorImage.parent as RectTransform;
        if (parent == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            Mouse.current.position.ReadValue(),
            null,
            out Vector2 localPoint
        );

        scissorsCursorImage.anchoredPosition = localPoint + cursorOffset;
    }

    private void UpdateProgress()
    {
        int total = 0;
        int completed = 0;

        foreach (var path in tracePaths)
        {
            total += path.TotalPoints;
            completed += path.CompletedPoints;
        }

        int percent = total == 0
            ? 0
            : Mathf.RoundToInt((float)completed / total * 100f);

        if (progressText != null)
            progressText.text = $"{percent}%";
    }

    private void CompleteTracing()
    {
        if (revealButton != null)
            revealButton.gameObject.SetActive(true);

        isTracing = false;
    }

    public void Close()
    {
        if (scissorsCursorImage != null)
            scissorsCursorImage.gameObject.SetActive(false);

        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void PlayCutSound()
    {
        if (audioSource == null || cutPointSound == null)
            return;

        audioSource.PlayOneShot(cutPointSound);
    }

    private void HideAllPoints()
    {
        foreach (var path in tracePaths)
            path.HideAllPoints();
    }
}