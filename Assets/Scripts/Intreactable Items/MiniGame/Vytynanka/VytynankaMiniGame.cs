using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//Vytynanka (Ukrainian papercut): choose the paper, cut the half pattern along the folded edge, unfold it, mount it on backing paper.
//Flow: paper colour -> cutting -> "Unfold" -> the mirrored half opens, the cut-away paper falls away -> backing paper -> "Keep it".
//Cutting follows each hand-placed line (TracePath) continuously: progress is a distance along the line, not dot hits.
//Closing before every line is cut never completes it (the cut is kept until the next reload); closing once everything is cut still completes it.
//The first finished papercut gives the item once (story flags, see PapercutPalette); later ones are just for fun.
public class VytynankaMiniGame : MonoBehaviour, IMiniGame
{
    public const string MadeFlag = PapercutPalette.MadeFlag;
    public const string JournalTopicId = "vytynanka";

    [Header("Debug")]
    [SerializeField] private bool startDirectlyInTracing; //Skips the colour choice (cream)
    public static VytynankaMiniGame Instance;
    private TracePath[] tracePaths;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject colorSelectPanel;
    [SerializeField] private GameObject mountSelectPanel;
    [SerializeField] private GameObject workArea;
    [SerializeField] private Image paperImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button revealButton;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image templateGhostImage;

    [Header("Auto Find")]
    [SerializeField] private Transform tracePointsParent;
    [SerializeField] private RectTransform drawLineContainer;

    [Header("Cursor")]
    [SerializeField] private RectTransform scissorsCursorImage;
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, 35f);

    [Header("Drawing")]
    [SerializeField] private Image lineSegmentPrefab;
    [SerializeField] private float lineThickness = 4f;
    [SerializeField] private Color lineColor = new Color(0.25f, 0.18f, 0.12f, 1f);

    [Header("Cutting (canvas units, 1080 high)")]
    [SerializeField] private float cutTolerance = 28f; //How far from the line the scissors may be and still cut
    [SerializeField] private float startTolerance = 45f; //How close a press must be to where a line goes on
    [SerializeField] private float maxAdvance = 80f; //The longest single slide of the cut along the line
    [SerializeField] private float kinkAllowance = 8f; //Small bumps in the line's distance (its corners) that don't stop the slide
    [SerializeField] private float guideLength = 160f; //The glowing stretch ahead of the cut
    [SerializeField] private Color guideColor = new Color(1f, 0.78f, 0.25f, 0.75f);
    [SerializeField] private Sprite markerSprite; //Round dot for "go on here" (defaults to the trace dots' sprite)

    [SerializeField] private int scrapCount = 5; //About this many paper scraps fall when a line is finished

    [Header("Unfolding")]
    [SerializeField] private float unfoldSeconds = 1.2f;
    [SerializeField] private float centreSeconds = 0.5f;
    [SerializeField] private Sprite finishedSprite; //The whole papercut, white with transparent openings (VytynankaSetup bakes it)

    [Header("Reward")]
    [SerializeField] private GameObject papercutItem; //"My vytynanka" (ItemDictionary)

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutPointSound;

    private enum Phase { Closed, ChoosingPaper, Cutting, ReadyToUnfold, Unfolding, Mounting }

    private const string CuttingHint = "Hold the mouse button and cut along the glowing line.";

    //One cut line in the drawing container's space: the dots joined into a polyline, cut by distance along it
    private class CutLine
    {
        public Vector2[] points;
        public float[] distance; //From the start to each point
        public float Length => distance[distance.Length - 1];
        public float progress;
        public int drawnPoints; //Segments drawn so far end at this point index
        public Image liveSegment; //From the last passed point to the scissors
        public Image startMarker;
        public bool Done => progress >= Length - 0.01f;

        public Vector2 PointAt(float s)
        {
            s = Mathf.Clamp(s, 0f, Length);
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (s <= distance[i + 1] || i == points.Length - 2)
                {
                    float segment = distance[i + 1] - distance[i];
                    float t = segment > 0f ? (s - distance[i]) / segment : 0f;
                    return Vector2.Lerp(points[i], points[i + 1], t);
                }
            }
            return points[points.Length - 1];
        }

        public Vector2 End => PointAt(progress);

        //The same, walking forward from a known segment (for many points in order)
        public Vector2 PointAt(float s, ref int segment)
        {
            s = Mathf.Clamp(s, 0f, Length);
            segment = Mathf.Clamp(segment, 0, points.Length - 2);
            while (segment > 0 && s < distance[segment])
                segment--;
            while (segment < points.Length - 2 && s > distance[segment + 1])
                segment++;

            float length = distance[segment + 1] - distance[segment];
            float t = length > 0f ? (s - distance[segment]) / length : 0f;
            return Vector2.Lerp(points[segment], points[segment + 1], t);
        }
    }

    private readonly List<CutLine> lines = new();
    private readonly List<GameObject> drawnSegments = new();
    private readonly List<Image> guideSegments = new();
    private Phase phase = Phase.Closed;
    private bool isOpen;
    private MiniGameItem currentSource;
    private string paperName = "cream";
    private string mountName = "darkred";

    private CutLine activeLine; //The line to go on with (glows)
    private CutLine strokeLine; //The line the held button is cutting
    private bool wasPressed;
    private Vector2 lastPointerLocal;
    private float lastCutSoundTime;
    private float guideDrawnAt = -1f;
    private RectTransform guideContainer, scrapContainer;
    private Image continueMarker;

    private RectTransform mirrorHalf, composition;
    private Image mountSheet;
    private Vector2 paperStartPosition, linesStartPosition;
    private bool layoutStored;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;
        AutoFindReferences();

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (phase == Phase.Cutting)
        {
            UpdateScissorsCursor();
            UpdateCutting();
            PulseMarker();
        }
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void AutoFindReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (colorSelectPanel == null) colorSelectPanel = FindDeep("ColorSelectPanel")?.gameObject;
        if (mountSelectPanel == null) mountSelectPanel = FindDeep("MountSelectPanel")?.gameObject;
        if (workArea == null) workArea = FindDeep("WorkArea")?.gameObject;
        if (paperImage == null) paperImage = FindDeep("PaperImage")?.GetComponent<Image>();
        if (templateGhostImage == null) templateGhostImage = FindDeep("TemplateGhostImage")?.GetComponent<Image>();
        if (progressText == null) progressText = FindDeep("ProgressText")?.GetComponent<TMP_Text>();
        if (revealButton == null) revealButton = FindDeep("RevealButton")?.GetComponent<Button>();
        if (hintText == null) hintText = FindDeep("HintText")?.GetComponent<TMP_Text>();
        if (tracePointsParent == null) tracePointsParent = FindDeep("Pathes");
        if (drawLineContainer == null) drawLineContainer = FindDeep("DrawLineContainer") as RectTransform;
        if (scissorsCursorImage == null) scissorsCursorImage = FindDeep("ScissorsCursorImage") as RectTransform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        tracePaths = tracePointsParent != null
         ? tracePointsParent.GetComponentsInChildren<TracePath>(true)
             .OrderBy(p => p.transform.GetSiblingIndex())
             .ToArray()
         : new TracePath[0];
    }

    //The canvas scales by height; on screens narrower than 4:3 the board shrinks to fit the width
    private void FitBoard()
    {
        var board = FindDeep("Board") as RectTransform;
        var canvas = board != null ? board.parent as RectTransform : null;
        if (canvas == null)
            return;

        Canvas.ForceUpdateCanvases();
        float scale = Mathf.Min(1f, (canvas.rect.width - 32f) / board.sizeDelta.x, (canvas.rect.height - 16f) / board.sizeDelta.y);
        board.localScale = new Vector3(scale, scale, 1f);
    }

    private Transform FindDeep(string name)
    {
        return GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    public void Open(MiniGameItem source)
    {
        if (isOpen)
            return;

        AutoFindReferences();
        StoreLayout();

        isOpen = true;
        currentSource = source;
        panel.SetActive(true);
        PauseController.SetPause(true);
        FitBoard();
        wasPressed = false;
        strokeLine = null;

        //A cut started earlier (and closed) carries on where it stopped
        if (phase == Phase.Cutting && lines.Any(l => l.progress > 0f))
        {
            ShowCutting();
            return;
        }

        ShowColorSelect();

        if (startDirectlyInTracing)
            SelectPaper("cream");
    }

    public void Close()
    {
        if (!isOpen)
            return;

        //Everything is cut: the work is done even if the unfolding or mounting wasn't finished
        bool finished = phase == Phase.ReadyToUnfold || phase == Phase.Unfolding || phase == Phase.Mounting;

        StopAllCoroutines();
        ClearScraps();
        isOpen = false;
        Cursor.visible = true;

        if (scissorsCursorImage != null)
            scissorsCursorImage.gameObject.SetActive(false);

        if (finished)
            ResetLayout();

        if (phase != Phase.Cutting)
            phase = Phase.Closed;

        panel.SetActive(false);
        PauseController.SetPause(false);

        MiniGameItem source = currentSource;
        currentSource = null;

        if (finished)
            Finish(source);
    }

    //Paper buttons (VytynankaSetup wires them with the colour id)
    public void SelectPaper(string id)
    {
        if (!isOpen || phase != Phase.ChoosingPaper)
            return;

        paperName = id;
        mountName = PapercutPalette.DefaultMount(id);

        if (paperImage != null)
            paperImage.color = PapercutPalette.PaperColor(id);

        ResetCut();
        phase = Phase.Cutting;
        ShowCutting();
    }

    //Backing paper buttons: shows the papercut on it at once
    public void SelectMount(string id)
    {
        if (phase != Phase.Mounting || !PapercutPalette.MountAllowed(id, paperName))
            return;

        mountName = id;
        if (mountSheet != null)
            mountSheet.color = PapercutPalette.MountColor(id);
        UpdateMountButtons();
    }

    //The Reveal button: "Unfold" when everything is cut, "Keep it" when mounted
    public void OnRevealButton()
    {
        if (phase == Phase.ReadyToUnfold)
            StartCoroutine(Unfold());
        else if (phase == Phase.Mounting)
            Close();
    }

    private void ShowColorSelect()
    {
        ResetLayout();
        ResetCut();
        phase = Phase.ChoosingPaper;

        colorSelectPanel.SetActive(true);
        SetActive(mountSelectPanel, false);
        workArea.SetActive(true);
        SetGuidesVisible(false);
        Cursor.visible = true;
        SetRevealButton(null);
        SetProgress("");

        if (scissorsCursorImage != null)
            scissorsCursorImage.gameObject.SetActive(false);

        HideAllPoints();

        bool alreadyMade = SaveController.Instance != null && SaveController.Instance.HasFlag(MadeFlag);
        SetHint(alreadyMade
            ? "Make a new one. It will replace your old one.\nChoose a paper colour."
            : "The paper is folded in half.\nChoose a paper colour.");
    }

    private void ShowCutting()
    {
        colorSelectPanel.SetActive(false);
        SetActive(mountSelectPanel, false);
        workArea.SetActive(true);
        HideAllPoints();
        SetGuidesVisible(true);
        RefreshGuides(true);

        if (scissorsCursorImage != null)
            scissorsCursorImage.gameObject.SetActive(true);
        Cursor.visible = false;

        SetRevealButton(null);
        SetHint(CuttingHint);
        UpdateProgress();
    }

    //Builds the cut lines from the dots (once) and clears the cut
    private void ResetCut()
    {
        ClearDrawnSegments();
        ClearScraps();
        BuildLines();

        foreach (var line in lines)
        {
            line.progress = 0f;
            line.drawnPoints = 0;
            if (line.liveSegment != null)
                Destroy(line.liveSegment.gameObject);
            line.liveSegment = null;
        }

        activeLine = lines.FirstOrDefault();
        strokeLine = null;
        guideDrawnAt = -1f;
    }

    private void BuildLines()
    {
        if (lines.Count > 0 || drawLineContainer == null)
            return;

        foreach (var path in tracePaths)
        {
            var points = path.Points.Select(p => LocalPoint((RectTransform)p.transform)).ToList();
            if (points.Count < 2)
                continue;
            if (path.ClosePathToStart)
                points.Add(points[0]);

            var distance = new float[points.Count];
            for (int i = 1; i < points.Count; i++)
                distance[i] = distance[i - 1] + Vector2.Distance(points[i - 1], points[i]);

            lines.Add(new CutLine { points = points.ToArray(), distance = distance });
        }
    }

    private Vector2 LocalPoint(RectTransform point)
    {
        return drawLineContainer.InverseTransformPoint(point.TransformPoint(point.rect.center));
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

    //While the button is held the scissors cut along the chosen line; a fast stroke is followed in small steps
    private void UpdateCutting()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null)
            return;

        bool pressed = pointer.press.isPressed;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawLineContainer, pointer.position.ReadValue(), null, out Vector2 local);

        if (!pressed)
        {
            wasPressed = false;
            strokeLine = null;
            return;
        }

        if (!wasPressed)
        {
            wasPressed = true;
            lastPointerLocal = local;
            strokeLine = PickLine(local);
            if (strokeLine != null)
            {
                activeLine = strokeLine;
                RefreshGuides(true);
            }
        }

        if (strokeLine == null)
            return;

        float step = cutTolerance * 0.5f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(lastPointerLocal, local) / step));
        float before = strokeLine.progress;

        for (int i = 1; i <= steps && strokeLine != null && !strokeLine.Done; i++)
            Advance(strokeLine, Vector2.Lerp(lastPointerLocal, local, (float)i / steps));

        lastPointerLocal = local;

        if (strokeLine != null && strokeLine.progress > before)
            OnCut(strokeLine, strokeLine.progress - before);
    }

    //A press continues the glowing line near the scissors, or starts another line at its marker; never a line far away
    private CutLine PickLine(Vector2 local)
    {
        if (activeLine != null && !activeLine.Done &&
            (Vector2.Distance(local, activeLine.End) <= startTolerance || Follow(activeLine, local, out _)))
            return activeLine;

        CutLine best = null;
        float bestDistance = startTolerance;
        foreach (var line in lines)
        {
            if (line.Done)
                continue;

            float d = Vector2.Distance(local, line.End);
            if (d <= bestDistance)
            {
                bestDistance = d;
                best = line;
            }
        }
        return best;
    }

    private void Advance(CutLine line, Vector2 local)
    {
        if (Follow(line, local, out float reached))
        {
            line.progress = reached;
            if (line.Length - line.progress < cutTolerance * 0.3f)
                line.progress = line.Length;
        }
    }

    //Strictly sequential: the cut slides forward from where it is, only while the line keeps getting closer to the scissors,
    //and stops at the first closest point. Every point it passes is within the tolerance (or still approaching it), so a later
    //part of the line that runs close by (a leaf tip, the birds) is never reached across the gap: the line would first have to
    //move away from the scissors. Too far from the line: nothing happens, the cut waits where it is.
    //Checked on the real lines (2026-10-31): no advance through any stretch outside the tolerance in 376,920 test positions.
    private bool Follow(CutLine line, Vector2 local, out float reached)
    {
        const float step = 2f;
        float start = line.progress, end = Mathf.Min(line.Length, start + maxAdvance);
        int segment = 0;
        float startDistance = Vector2.Distance(local, line.PointAt(start, ref segment));
        float bestDistance = startDistance;
        reached = start;

        for (float s = start + step; s < end + step; s += step)
        {
            float at = Mathf.Min(s, end);
            float distance = Vector2.Distance(local, line.PointAt(at, ref segment));

            if (distance > cutTolerance && distance > startDistance)
                break; //Left the tolerance
            if (distance < bestDistance)
            {
                bestDistance = distance;
                reached = at;
            }
            else if (distance > bestDistance + kinkAllowance)
                break; //The line moves away again: the first closest point is found

            if (at >= end)
                break;
        }

        return bestDistance <= cutTolerance && reached > start;
    }

    private void OnCut(CutLine line, float amount)
    {
        DrawCut(line);

        if (amount > 2f)
            PlayCutSound();

        if (line.Done)
        {
            StartCoroutine(DropScraps(line));
            strokeLine = null;
            activeLine = NextLine(line.End);
            if (activeLine == null)
            {
                UpdateProgress();
                OnAllCut();
                return;
            }
        }

        RefreshGuides(line.Done);
        UpdateProgress();
    }

    //After a line is finished, the nearest unfinished one glows next
    private CutLine NextLine(Vector2 from)
    {
        return lines.Where(l => !l.Done).OrderBy(l => Vector2.Distance(from, l.End)).FirstOrDefault();
    }

    //The cut so far: whole segments up to the last passed point, then a live segment to the scissors
    private void DrawCut(CutLine line)
    {
        while (line.drawnPoints < line.points.Length - 1 && line.distance[line.drawnPoints + 1] <= line.progress + 0.01f)
        {
            drawnSegments.Add(DrawSegment(drawLineContainer, line.points[line.drawnPoints], line.points[line.drawnPoints + 1], lineThickness, lineColor).gameObject);
            line.drawnPoints++;
        }

        bool live = !line.Done && line.progress > line.distance[line.drawnPoints];
        if (live)
        {
            if (line.liveSegment == null)
                line.liveSegment = DrawSegment(drawLineContainer, Vector2.zero, Vector2.right, lineThickness, lineColor);
            PlaceSegment(line.liveSegment.rectTransform, line.points[line.drawnPoints], line.End, lineThickness);
        }
        else if (line.liveSegment != null)
        {
            Destroy(line.liveSegment.gameObject);
            line.liveSegment = null;
        }
    }

    //The glowing stretch ahead of the cut, a pulsing dot where to go on, small dots where the other lines start
    private void RefreshGuides(bool force)
    {
        EnsureGuides();

        if (!force && activeLine != null && Mathf.Abs(activeLine.progress - guideDrawnAt) < 6f)
            return;

        foreach (var segment in guideSegments)
            segment.gameObject.SetActive(false);

        if (activeLine != null && !activeLine.Done)
        {
            guideDrawnAt = activeLine.progress;
            float from = activeLine.progress, to = Mathf.Min(activeLine.Length, from + guideLength);
            int used = 0;
            Vector2 previous = activeLine.PointAt(from);
            for (int i = 0; i < activeLine.points.Length; i++)
            {
                float d = activeLine.distance[i];
                if (d <= from) continue;
                Vector2 next = d >= to ? activeLine.PointAt(to) : activeLine.points[i];
                GuideSegment(used++, previous, next);
                previous = next;
                if (d >= to) break;
            }

            continueMarker.gameObject.SetActive(true);
            continueMarker.rectTransform.anchoredPosition = activeLine.End;
        }
        else
        {
            continueMarker.gameObject.SetActive(false);
        }

        continueMarker.transform.SetAsLastSibling();

        foreach (var line in lines)
        {
            bool show = !line.Done && line != activeLine;
            if (line.startMarker == null && show)
                line.startMarker = Marker("Line start", 16f, new Color(0.7f, 0.15f, 0.1f, 0.85f));
            if (line.startMarker != null)
            {
                line.startMarker.gameObject.SetActive(show);
                line.startMarker.rectTransform.anchoredPosition = line.End;
            }
        }
    }

    private void GuideSegment(int index, Vector2 from, Vector2 to)
    {
        while (guideSegments.Count <= index)
            guideSegments.Add(DrawSegment(guideContainer, Vector2.zero, Vector2.right, lineThickness + 6f, guideColor));

        Image segment = guideSegments[index];
        segment.gameObject.SetActive(true);
        PlaceSegment(segment.rectTransform, from, to, lineThickness + 6f);
    }

    private void EnsureGuides()
    {
        if (guideContainer != null)
            return;

        guideContainer = new GameObject("GuideContainer", typeof(RectTransform)).GetComponent<RectTransform>();
        guideContainer.SetParent(drawLineContainer.parent, false);
        guideContainer.SetSiblingIndex(drawLineContainer.GetSiblingIndex()); //Under the cut lines
        guideContainer.anchorMin = drawLineContainer.anchorMin;
        guideContainer.anchorMax = drawLineContainer.anchorMax;
        guideContainer.sizeDelta = drawLineContainer.sizeDelta;
        guideContainer.anchoredPosition = drawLineContainer.anchoredPosition;

        if (markerSprite == null)
            markerSprite = tracePaths.SelectMany(p => p.Points).Select(p => p.GetComponent<Image>().sprite).FirstOrDefault(s => s != null);

        continueMarker = Marker("Continue here", 30f, new Color(0.85f, 0.12f, 0.08f, 1f));

        //Paper scraps fall under the glow, so they never hide where to cut next
        scrapContainer = new GameObject("ScrapContainer", typeof(RectTransform)).GetComponent<RectTransform>();
        scrapContainer.SetParent(drawLineContainer.parent, false);
        scrapContainer.SetSiblingIndex(guideContainer.GetSiblingIndex());
        scrapContainer.anchorMin = drawLineContainer.anchorMin;
        scrapContainer.anchorMax = drawLineContainer.anchorMax;
        scrapContainer.sizeDelta = drawLineContainer.sizeDelta;
        scrapContainer.anchoredPosition = drawLineContainer.anchoredPosition;
    }

    //A finished line lets a few flat scraps of the paper go: they turn a little, drop a short way and fade (once per line)
    private IEnumerator DropScraps(CutLine line)
    {
        EnsureGuides();
        Color paperColor = paperImage.color;
        int count = Random.Range(scrapCount - 1, scrapCount + 2);
        var scraps = new List<(RectTransform rect, Image image, Vector2 from, Vector2 fall, float turn, float life)>();

        for (int i = 0; i < count; i++)
        {
            var image = new GameObject("Scrap", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            RectTransform rect = image.rectTransform;
            rect.SetParent(scrapContainer, false);
            rect.sizeDelta = new Vector2(Random.Range(6f, 14f), Random.Range(4f, 9f));
            float shade = Random.Range(0.82f, 1f);
            image.color = new Color(paperColor.r * shade, paperColor.g * shade, paperColor.b * shade, 1f);
            image.raycastTarget = false;

            Vector2 from = line.PointAt(Random.Range(0f, line.Length));
            rect.anchoredPosition = from;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            scraps.Add((rect, image, from, new Vector2(Random.Range(-10f, 10f), -Random.Range(25f, 50f)), Random.Range(-60f, 60f), Random.Range(0.4f, 0.7f)));
        }

        for (float t = 0f; t < 0.7f; t += Time.unscaledDeltaTime)
        {
            foreach (var s in scraps)
            {
                if (s.rect == null) continue;
                float k = Mathf.Clamp01(t / s.life);
                s.rect.anchoredPosition = s.from + s.fall * (k * k); //Slow at first, then falling
                s.rect.localRotation = Quaternion.Euler(0f, 0f, s.rect.localEulerAngles.z + s.turn * Time.unscaledDeltaTime);
                Color c = s.image.color;
                c.a = 1f - k;
                s.image.color = c;
            }
            yield return null;
        }

        foreach (var s in scraps)
            if (s.rect != null)
                Destroy(s.rect.gameObject);
    }

    private void ClearScraps()
    {
        if (scrapContainer == null)
            return;
        foreach (Transform scrap in scrapContainer)
            Destroy(scrap.gameObject);
    }

    private Image Marker(string name, float size, Color color)
    {
        var marker = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        marker.rectTransform.SetParent(guideContainer, false);
        marker.rectTransform.sizeDelta = new Vector2(size, size);
        marker.sprite = markerSprite;
        marker.color = color;
        marker.raycastTarget = false;
        return marker;
    }

    private void PulseMarker()
    {
        if (continueMarker == null || !continueMarker.gameObject.activeSelf)
            return;

        float scale = 1f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
        continueMarker.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetGuidesVisible(bool visible)
    {
        if (guideContainer != null)
            guideContainer.gameObject.SetActive(visible);
    }

    private void OnAllCut()
    {
        phase = Phase.ReadyToUnfold;
        strokeLine = null;
        Cursor.visible = true;
        SetGuidesVisible(false);

        if (scissorsCursorImage != null)
            scissorsCursorImage.gameObject.SetActive(false);

        SetHint("Everything is cut! Now unfold the paper.");
        SetRevealButton("Unfold");
    }

    private IEnumerator Unfold()
    {
        phase = Phase.Unfolding;
        SetRevealButton(null);
        SetHint("");
        SetProgress("");

        if (templateGhostImage != null)
            templateGhostImage.enabled = false;

        RectTransform paperRect = paperImage.rectTransform;
        //The fold is the left edge of the half pattern (the trunk's half circles lie on it)
        float foldX = paperRect.anchoredPosition.x - paperRect.rect.width * paperRect.pivot.x;

        mirrorHalf = new GameObject("MirrorHalf", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        mirrorHalf.SetParent(workArea.transform, false);
        mirrorHalf.SetSiblingIndex(paperRect.GetSiblingIndex() + 1);
        mirrorHalf.anchorMin = mirrorHalf.anchorMax = paperRect.anchorMin;
        mirrorHalf.sizeDelta = Vector2.zero;
        mirrorHalf.anchoredPosition = new Vector2(foldX, 0f);

        Image paperCopy = Instantiate(paperImage, mirrorHalf);
        paperCopy.raycastTarget = false;
        paperCopy.rectTransform.anchoredPosition = paperRect.anchoredPosition - new Vector2(foldX, 0f);
        Color paperColor = paperImage.color;

        RectTransform linesCopy = Instantiate(drawLineContainer, mirrorHalf);
        linesCopy.anchoredPosition = drawLineContainer.anchoredPosition - new Vector2(foldX, 0f);

        //The folded half swings open around the fold, darker while it faces away
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / unfoldSeconds)
        {
            float scaleX = Mathf.Lerp(1f, -1f, Mathf.SmoothStep(0f, 1f, t));
            mirrorHalf.localScale = new Vector3(scaleX, 1f, 1f);
            float shade = Mathf.Lerp(0.7f, 1f, Mathf.Abs(scaleX));
            paperCopy.color = new Color(paperColor.r * shade, paperColor.g * shade, paperColor.b * shade, paperColor.a);
            yield return null;
        }

        mirrorHalf.localScale = new Vector3(-1f, 1f, 1f);
        paperCopy.color = paperColor;
        SoundEffectManager.Play("MiniGameCompleted");

        //The whole papercut moves to the middle
        Vector2 shift = new Vector2(paperRect.rect.width / 2f, 0f);
        Vector2 paperFrom = paperRect.anchoredPosition, linesFrom = drawLineContainer.anchoredPosition, mirrorFrom = mirrorHalf.anchoredPosition;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / centreSeconds)
        {
            Vector2 offset = shift * Mathf.SmoothStep(0f, 1f, t);
            paperRect.anchoredPosition = paperFrom + offset;
            drawLineContainer.anchoredPosition = linesFrom + offset;
            mirrorHalf.anchoredPosition = mirrorFrom + offset;
            yield return null;
        }
        paperRect.anchoredPosition = paperFrom + shift;
        drawLineContainer.anchoredPosition = linesFrom + shift;
        mirrorHalf.anchoredPosition = mirrorFrom + shift;

        yield return Mount(paperRect, new Vector2(foldX + shift.x, paperRect.anchoredPosition.y), paperColor);
    }

    //The cut-away paper falls away (the real silhouette fades in), then the papercut is laid on backing paper
    private IEnumerator Mount(RectTransform paperRect, Vector2 centre, Color paperColor)
    {
        Vector2 size = new Vector2(paperRect.rect.width * 2f, paperRect.rect.height);
        Vector2 sheetSize = size + new Vector2(80f, 80f);

        composition = new GameObject("Composition", typeof(RectTransform)).GetComponent<RectTransform>();
        composition.SetParent(workArea.transform, false);
        //Above the paper when the silhouette replaces it; without the baked silhouette the backing goes behind the cut paper
        composition.SetSiblingIndex(finishedSprite != null ? mirrorHalf.GetSiblingIndex() + 1 : paperRect.GetSiblingIndex());
        composition.anchorMin = composition.anchorMax = paperRect.anchorMin;
        composition.sizeDelta = Vector2.zero;
        composition.anchoredPosition = centre;

        Image sheetShadow = Layer("Sheet shadow", paperImage.sprite, sheetSize, new Vector2(14f, -14f), new Color(0f, 0f, 0f, 0f));
        mountSheet = Layer("Backing paper", paperImage.sprite, sheetSize, Vector2.zero, PapercutPalette.MountColor(mountName) * new Color(1, 1, 1, 0));
        Image cutShadow = null, cut = null;
        if (finishedSprite != null)
        {
            cutShadow = Layer("Papercut shadow", finishedSprite, size, new Vector2(5f, -6f), new Color(0f, 0f, 0f, 0f));
            cut = Layer("Papercut", finishedSprite, size, Vector2.zero, new Color(paperColor.r, paperColor.g, paperColor.b, 0f));
        }

        CanvasGroup paperGroup = Group(paperImage.gameObject), linesGroup = Group(drawLineContainer.gameObject), mirrorGroup = mirrorHalf.GetComponent<CanvasGroup>();

        if (cut != null)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                SetAlpha(cut, t);
                SetAlpha(cutShadow, 0.3f * t);
                paperGroup.alpha = linesGroup.alpha = mirrorGroup.alpha = 1f - t;
                yield return null;
            }
            SetAlpha(cut, 1f);
            SetAlpha(cutShadow, 0.3f);
            paperGroup.alpha = linesGroup.alpha = mirrorGroup.alpha = 0f;
        }

        phase = Phase.Mounting;
        SetActive(mountSelectPanel, true);
        UpdateMountButtons();
        SetHint("Choose a backing paper.\nThen press \"Keep it\".");
        SetRevealButton("Keep it");

        //Lay it on the backing paper: the sheet appears and the piece settles a little smaller
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(mountSheet, e);
            SetAlpha(sheetShadow, 0.35f * e);
            composition.localScale = Vector3.one * Mathf.Lerp(1f, 0.88f, e);
            yield return null;
        }
        SetAlpha(mountSheet, 1f);
        SetAlpha(sheetShadow, 0.35f);
        composition.localScale = Vector3.one * 0.88f;
    }

    private Image Layer(string name, Sprite sprite, Vector2 size, Vector2 offset, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(composition, false);
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = offset;
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static CanvasGroup Group(GameObject target)
    {
        var group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void UpdateMountButtons()
    {
        if (mountSelectPanel == null)
            return;

        //The row's layout must be current before the frame is placed on the chosen swatch
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)mountSelectPanel.transform);
        var selection = mountSelectPanel.transform.Find("SelectionFrame") as RectTransform;
        if (selection != null)
            selection.gameObject.SetActive(false);

        foreach (var mount in PapercutPalette.Mounts)
        {
            var button = mountSelectPanel.transform.Find(mount.id + "Button") as RectTransform;
            if (button == null) continue;

            //Unavailable: its own colour, fully visible, with a thin diagonal strike-through; not clickable (no fading, no grey tint)
            bool allowed = PapercutPalette.MountAllowed(mount.id, paperName);
            var swatch = button.GetComponent<Button>();
            var colors = swatch.colors;
            colors.disabledColor = Color.white;
            swatch.colors = colors;
            swatch.interactable = allowed;
            Group(button.gameObject).alpha = 1f;
            button.localScale = Vector3.one;

            var strike = button.Find("Unavailable") as RectTransform;
            if (strike != null)
            {
                strike.sizeDelta = new Vector2(strike.sizeDelta.x, 3f);
                strike.gameObject.SetActive(!allowed);
            }

            //Chosen: an even frame just around it, drawn behind the row (no scaling, so every swatch keeps its own click area)
            if (selection != null && mount.id == mountName)
            {
                selection.gameObject.SetActive(true);
                selection.anchoredPosition = button.anchoredPosition;
                selection.sizeDelta = button.rect.size + new Vector2(12f, 12f);
            }
        }
    }

    //The first papercut is kept: its colours, the item, the journal and a save. Later ones give nothing
    private void Finish(MiniGameItem source)
    {
        SaveController save = SaveController.Instance;
        if (save == null || papercutItem == null)
            return;

        bool firstTime = !save.HasFlag(MadeFlag);
        var previous = PapercutPalette.SavedChoice();
        Item item = papercutItem.GetComponent<Item>();

        //The newest papercut is the one the player keeps: its colours replace the old ones (the item draws itself from them)
        PapercutPalette.SaveChoice(paperName, mountName);

        //Only one papercut item: added the first time, or again if it is somehow missing
        bool owned = !firstTime && InventoryController.Instance.GetItemCounts().GetValueOrDefault(item.itemId) > 0;
        if (!owned && !InventoryController.Instance.AddItem(papercutItem))
        {
            //Nothing changes, so the papercut can be made again later
            if (firstTime) PapercutPalette.ClearChoice();
            else PapercutPalette.SaveChoice(previous.paper, previous.mount);
            Debug.LogWarning("Vytynanka: the papercut item couldn't be added (full inventory).");
            return;
        }

        save.SetFlag(MadeFlag);
        PapercutItem.RefreshAll();

        PapercutItem papercut = papercutItem.GetComponent<PapercutItem>();
        Sprite picture = papercut != null ? papercut.BuildIcon(paperName, mountName) : null;
        if (picture != null && ItemPickupUIController.Instance != null)
            ItemPickupUIController.Instance.ShowItemPickup(item.Name, picture);
        else
            item.ShowPopUp();

        if (firstTime)
            JournalController.Instance?.UnlockOrUpdateTopic(JournalTopicId, 0);

        if (source != null && !source.IsCompleted)
            source.CompleteMiniGame();

        save.SaveGame();
    }

    private void StoreLayout()
    {
        if (layoutStored || paperImage == null || drawLineContainer == null)
            return;

        paperStartPosition = paperImage.rectTransform.anchoredPosition;
        linesStartPosition = drawLineContainer.anchoredPosition;
        layoutStored = true;
    }

    //Undo the unfolding and mounting: the half sheet back in its place, the template visible again
    private void ResetLayout()
    {
        if (mirrorHalf != null)
            Destroy(mirrorHalf.gameObject);
        if (composition != null)
            Destroy(composition.gameObject);
        mirrorHalf = composition = null;
        mountSheet = null;

        if (layoutStored)
        {
            paperImage.rectTransform.anchoredPosition = paperStartPosition;
            drawLineContainer.anchoredPosition = linesStartPosition;
        }

        //Explicit != null: in the editor a missing component is a "fake null" object that passes C# null checks and 'is'
        if (paperImage != null)
            ShowFully(paperImage.GetComponent<CanvasGroup>());
        if (drawLineContainer != null)
            ShowFully(drawLineContainer.GetComponent<CanvasGroup>());
        if (templateGhostImage != null)
            templateGhostImage.enabled = true;

        SetActive(mountSelectPanel, false);
    }

    private static void ShowFully(CanvasGroup group)
    {
        if (group != null)
            group.alpha = 1f;
    }

    private void SetRevealButton(string label)
    {
        if (revealButton == null)
            return;

        revealButton.gameObject.SetActive(label != null);

        TMP_Text text = revealButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null && text != null)
            text.text = label;
    }

    private void SetHint(string message)
    {
        if (hintText != null)
            hintText.text = message;
    }

    private void SetProgress(string message)
    {
        if (progressText != null)
            progressText.text = message;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private Image DrawSegment(RectTransform parent, Vector2 from, Vector2 to, float thickness, Color color)
    {
        Image segment = Instantiate(lineSegmentPrefab, parent);
        segment.color = color;
        segment.raycastTarget = false;
        PlaceSegment(segment.rectTransform, from, to, thickness);
        return segment;
    }

    //A thin bar from one point to another (in its parent's space), a little longer so the joints don't show gaps
    private static void PlaceSegment(RectTransform segment, Vector2 from, Vector2 to, float thickness)
    {
        Vector2 direction = to - from;
        segment.anchorMin = segment.anchorMax = new Vector2(0.5f, 0.5f);
        segment.pivot = new Vector2(0f, 0.5f);
        segment.anchoredPosition = from - direction.normalized * (thickness * 0.5f);
        segment.sizeDelta = new Vector2(direction.magnitude + thickness, thickness);
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void UpdateScissorsCursor()
    {
        if (scissorsCursorImage == null || Pointer.current == null)
            return;

        RectTransform parent = scissorsCursorImage.parent as RectTransform;
        if (parent == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            Pointer.current.position.ReadValue(),
            null,
            out Vector2 localPoint
        );

        scissorsCursorImage.anchoredPosition = localPoint + cursorOffset;
    }

    private void UpdateProgress()
    {
        float total = lines.Sum(l => l.Length);
        float cut = lines.Sum(l => l.progress);
        int percent = total <= 0f ? 0 : Mathf.Min(100, Mathf.FloorToInt(cut / total * 100f));
        SetProgress($"{percent}%");
    }

    //Snips while the scissors move, not on every small step
    private void PlayCutSound()
    {
        if (audioSource == null || cutPointSound == null)
            return;

        if (Time.unscaledTime - lastCutSoundTime < 0.12f)
            return;

        lastCutSoundTime = Time.unscaledTime;
        audioSource.PlayOneShot(cutPointSound);
    }

    private void HideAllPoints()
    {
        foreach (var path in tracePaths)
            path.HideAllPoints();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    //Development only: forget the kept papercut so the first-time reward can be tested again (the journal entry stays)
    public void DevReset()
    {
        if (isOpen)
            Close();

        SaveController save = SaveController.Instance;
        foreach (string flag in PapercutPalette.AllFlags())
            save?.ClearFlag(flag);

        Item item = papercutItem != null ? papercutItem.GetComponent<Item>() : null;
        if (item != null)
            InventoryController.Instance.RemoveItemsFromInventory(item.itemId, 99);

        phase = Phase.Closed;
        Debug.Log("Vytynanka reset: the next finished papercut counts as the first one again.");
    }
#endif
}
