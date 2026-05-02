using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CinematicTextController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private CanvasGroup textCanvasGroup;
    [SerializeField] private TMP_Text cinematicText;

    [Header("Sequence")]
    [SerializeField] private CinematicSequenceSO sequence;

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool allowMouseClick = true;
    [SerializeField] private bool allowSpaceKey = true;
    [SerializeField] private bool allowEnterKey = true;
    [SerializeField] private bool holdInputAfterAction = true;
    [SerializeField] private float inputBlockTime = 0.1f;
    [SerializeField] private float delayBetweenLines = 0.05f;
    [SerializeField] private float delayBetweenSlides = 0.2f;

    [Header("Typing")]
    [SerializeField] private float typingSpeed = 0.025f;
    [SerializeField] private AudioClip[] voiceSounds;
    [SerializeField] private float voiceVolume = 1f;
    [SerializeField] private Vector2 voicePitchRange = new Vector2(0.98f, 1.03f);
    [SerializeField] private Vector2 voiceIntervalRange = new Vector2(0.04f, 0.08f);
    [SerializeField] private float fastTypingMultiplier = 12f;
    [SerializeField] private float minFastTypingDelay = 0.0025f;
    private bool isFastForwarding;

    [Header("Fade")]
    [SerializeField] private float rootFadeInDuration = 0.8f;
    [SerializeField] private float rootFadeOutDuration = 0.8f;

    [Header("Events")]
    public UnityEvent onSequenceStarted;
    public UnityEvent onSequenceFinished;
    public UnityEvent onCharacterSelectionRequested;
    public StringEvent onCustomEventTriggered;

    private bool isPlaying;
    private bool waitingForInput;
    private bool inputLocked;
    private bool waitingForExternalContinue;

    private bool isTyping;
    private float nextVoiceTime;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (rootCanvasGroup != null)
            rootCanvasGroup.alpha = 0f;

        if (textCanvasGroup != null)
            textCanvasGroup.alpha = 0f;

        if (cinematicText != null)
            cinematicText.text = string.Empty;
    }

    private void Start()
    {
        if (SaveController.Instance != null && SaveController.Instance.HasSeenIntro)
            return;

        if (playOnStart && sequence != null)
            Play(sequence);
    }

    private void Update()
    {
        if (!isPlaying || inputLocked)
            return;

        bool clicked = allowMouseClick &&
                       Mouse.current != null &&
                       Mouse.current.leftButton.wasPressedThisFrame;

        bool pressedSpace = allowSpaceKey &&
                            Keyboard.current != null &&
                            Keyboard.current.spaceKey.wasPressedThisFrame;

        bool pressedEnter = allowEnterKey &&
                            Keyboard.current != null &&
                            (Keyboard.current.enterKey.wasPressedThisFrame ||
                             Keyboard.current.numpadEnterKey.wasPressedThisFrame);

        if (!(clicked || pressedSpace || pressedEnter))
            return;

        if (isTyping)
        {
            isFastForwarding = true;
            return;
        }

        if (waitingForInput)
        {
            waitingForInput = false;
        }
    }

    public void Play(CinematicSequenceSO newSequence = null)
    {
        if (newSequence != null)
            sequence = newSequence;

        if (sequence == null || sequence.slides == null || sequence.slides.Count == 0)
        {
            Debug.LogWarning("CinematicTextController: Sequence is empty.");
            return;
        }

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void ResumeAfterExternalAction()
    {
        waitingForExternalContinue = false;
    }

    private IEnumerator PlayRoutine()
    {
        isPlaying = true;
        waitingForInput = false;
        waitingForExternalContinue = false;

        onSequenceStarted?.Invoke();

        if (rootCanvasGroup != null)
            yield return FadeCanvasGroup(rootCanvasGroup, 0f, 1f, rootFadeInDuration);

        if (textCanvasGroup != null)
            textCanvasGroup.alpha = 1f;

        foreach (var slide in sequence.slides)
        {
            yield return ShowSlideRoutine(slide);
        }

        if (rootCanvasGroup != null)
            yield return FadeCanvasGroup(rootCanvasGroup, rootCanvasGroup.alpha, 0f, rootFadeOutDuration);

        isPlaying = false;
        onSequenceFinished?.Invoke();
    }

    private IEnumerator ShowSlideRoutine(CinematicSlideData slide)
    {
        if (slide == null)
            yield break;

        cinematicText.text = string.Empty;

        if (slide.lines == null || slide.lines.Count == 0)
        {
            if (slide.triggerCharacterSelection)
            {
                waitingForExternalContinue = true;
                onCharacterSelectionRequested?.Invoke();
                yield return new WaitUntil(() => waitingForExternalContinue == false);
            }

            if (slide.triggerCustomEvent && !string.IsNullOrWhiteSpace(slide.customEventId))
            {
                onCustomEventTriggered?.Invoke(slide.customEventId);
            }

            if (delayBetweenSlides > 0f)
                yield return new WaitForSecondsRealtime(delayBetweenSlides);

            yield break;
        }

        for (int i = 0; i < slide.lines.Count; i++)
        {
            yield return TypeLineRoutine(slide.lines[i]);

            if (holdInputAfterAction)
                yield return LockInputRoutine(inputBlockTime);

            waitingForInput = true;
            yield return new WaitUntil(() => waitingForInput == false);

            if (delayBetweenLines > 0f && i < slide.lines.Count - 1)
                yield return new WaitForSecondsRealtime(delayBetweenLines);
        }

        if (slide.triggerCharacterSelection)
        {
            waitingForExternalContinue = true;
            onCharacterSelectionRequested?.Invoke();
            yield return new WaitUntil(() => waitingForExternalContinue == false);
        }

        if (slide.triggerCustomEvent && !string.IsNullOrWhiteSpace(slide.customEventId))
        {
            onCustomEventTriggered?.Invoke(slide.customEventId);
        }

        if (holdInputAfterAction)
            yield return LockInputRoutine(inputBlockTime);

        if (delayBetweenSlides > 0f)
            yield return new WaitForSecondsRealtime(delayBetweenSlides);
    }
    
    private IEnumerator TypeLineRoutine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            yield break;

        isTyping = true;
        isFastForwarding = false;

        string trimmedLine = line.Trim();
        cinematicText.text = string.Empty;

        nextVoiceTime = Time.unscaledTime;

        for (int i = 0; i < trimmedLine.Length; i++)
        {
            char letter = trimmedLine[i];
            cinematicText.text += letter;

            TryPlayVoice(letter);

            float currentDelay = typingSpeed;

            if (isFastForwarding)
                currentDelay = Mathf.Max(typingSpeed / fastTypingMultiplier, minFastTypingDelay);

            yield return new WaitForSecondsRealtime(currentDelay);
        }

        cinematicText.text = trimmedLine;

        isTyping = false;
        isFastForwarding = false;
    }

    private void TryPlayVoice(char letter)
    {
        if (char.IsWhiteSpace(letter))
            return;

        if (voiceSounds == null || voiceSounds.Length == 0)
            return;

        if (Time.unscaledTime < nextVoiceTime)
            return;

        AudioClip clip = voiceSounds[Random.Range(0, voiceSounds.Length)];
        if (clip == null)
            return;

        float pitch = Random.Range(voicePitchRange.x, voicePitchRange.y);

        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.PlayVoice(new AudioClipData(clip, voiceVolume), pitch);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, voiceVolume);
        }

        nextVoiceTime = Time.unscaledTime + Random.Range(voiceIntervalRange.x, voiceIntervalRange.y);
    }

    private IEnumerator LockInputRoutine(float duration)
    {
        inputLocked = true;
        yield return new WaitForSecondsRealtime(duration);
        inputLocked = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float time = 0f;
        cg.alpha = from;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
    }

    public void SetCinematicPanelRaycastState(bool blocksRaycasts)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.blocksRaycasts = blocksRaycasts;
            rootCanvasGroup.interactable = blocksRaycasts;
        }
    }
}

[System.Serializable]
public class StringEvent : UnityEvent<string> { }