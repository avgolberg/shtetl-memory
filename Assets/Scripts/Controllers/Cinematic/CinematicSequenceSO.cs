using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CinematicSequence", menuName = "Cinematic/Cinematic Sequence")]
public class CinematicSequenceSO : ScriptableObject
{
    public List<CinematicSlideData> slides = new();
}

[System.Serializable]
public class CinematicSlideData
{
    [TextArea(3, 8)]
    public List<string> lines = new();

    [Header("Timing")]
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 2f;
    public float lineFadeInDuration = 0.25f;
    public float delayBetweenLines = 0.05f;

    [Header("Special Events")]
    public bool triggerCharacterSelection = false;
    public bool triggerCustomEvent = false;
    public string customEventId;
}