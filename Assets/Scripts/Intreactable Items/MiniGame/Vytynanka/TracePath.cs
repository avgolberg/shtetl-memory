using System.Linq;
using UnityEngine;

public class TracePath : MonoBehaviour
{
    private TracePoint[] points;
    private int currentIndex;

    public bool Completed => points != null && currentIndex >= points.Length;
    public int TotalPoints => points?.Length ?? 0;
    public int CompletedPoints => Mathf.Clamp(currentIndex, 0, TotalPoints);
    public TracePoint LastCompletedPoint { get; private set; }

    [SerializeField] private bool closePathToStart;

    public bool ClosePathToStart => closePathToStart;
    public TracePoint FirstPoint => points != null && points.Length > 0 ? points[0] : null;

    //The hand-placed points in order: the geometry of this cut line (VytynankaMiniGame cuts along them continuously)
    public TracePoint[] Points
    {
        get
        {
            if (points == null) CachePoints();
            return points;
        }
    }

    private void Awake()
    {
        CachePoints();
    }

    public void CachePoints()
    {
        points = GetComponentsInChildren<TracePoint>(true)
            .OrderBy(p => p.transform.GetSiblingIndex())
            .ToArray();
    }

    public bool ContainsPoint(TracePoint point)
    {
        return points != null && points.Contains(point);
    }

    public void ResetPath()
    {
        CachePoints();

        currentIndex = 0;
        LastCompletedPoint = null;

        foreach (var point in points)
            point.ResetPoint();

        UpdateVisuals();
    }

    public bool TryCompletePoint(TracePoint point, out TracePoint previousPoint)
    {
        previousPoint = null;

        if (Completed || points == null || points.Length == 0)
            return false;

        if (point != points[currentIndex])
            return false;

        previousPoint = LastCompletedPoint;

        point.Complete();
        LastCompletedPoint = point;
        currentIndex++;

        UpdateVisuals();

        return true;
    }

    private void UpdateVisuals()
    {
        if (points == null)
            return;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].Completed)
                continue;

            if (i == currentIndex)
                points[i].SetCurrent();
            else
                points[i].SetFuture();
        }
    }

    public void HideAllPoints()
    {
        CachePoints();

        foreach (var point in points)
            point.Hide();
    }
}