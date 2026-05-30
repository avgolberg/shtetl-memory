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
    [SerializeField] private TMP_Text centerText;
    [SerializeField] private TMP_Text letterText;

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

    [Header("Fade")]
    [SerializeField] private float rootFadeInDuration = 0.8f;
    [SerializeField] private float rootFadeOutDuration = 0.8f;

    [Header("Events")]
    public UnityEvent onSequenceStarted;
    public UnityEvent onSequenceFinished;
    public UnityEvent onCharacterSelectionRequested;
    public StringEvent onCustomEventTriggered;

    private bool keepLinesOnScreen;
    private TMP_Text activeText;

    private bool isPlaying;
    private bool waitingForInput;
    private bool inputLocked;
    private bool waitingForExternalContinue;

    private bool isTyping;
    private bool isFastForwarding;
    private float nextVoiceTime;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (rootCanvasGroup != null)
            rootCanvasGroup.alpha = 0f;

        if (textCanvasGroup != null)
            textCanvasGroup.alpha = 0f;

        HideAllTexts();
    }

    private void Start()
    {
        if (SaveController.Instance != null && SaveController.Instance.HasSeenIntro)
            return;

        if (playOnStart && sequence != null)
            Play(sequence, false);
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
            waitingForInput = false;
    }

    public void Play(CinematicSequenceSO newSequence = null)
    {
        Play(newSequence, false);
    }

    public void Play(CinematicSequenceSO newSequence = null, bool letterMode = false)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.gameObject.SetActive(true);
            rootCanvasGroup.blocksRaycasts = true;
            rootCanvasGroup.interactable = true;
        }

        if (textCanvasGroup != null)
        {
            textCanvasGroup.gameObject.SetActive(true);
            textCanvasGroup.blocksRaycasts = true;
            textCanvasGroup.interactable = true;
        }

        if (newSequence != null)
            sequence = newSequence;

        if (sequence == null || sequence.slides == null || sequence.slides.Count == 0)
        {
            Debug.LogWarning("CinematicTextController: Sequence is empty.");
            return;
        }

        ConfigureTextMode(letterMode);

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

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.gameObject.SetActive(false);
        }

        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;
            textCanvasGroup.blocksRaycasts = false;
            textCanvasGroup.interactable = false;
        }

        Time.timeScale = 1f;
        isPlaying = false;
        HideAllTexts();

        onSequenceFinished?.Invoke();
    }

    private IEnumerator ShowSlideRoutine(CinematicSlideData slide)
    {
        if (slide == null || activeText == null)
            yield break;

        activeText.text = string.Empty;

        if (slide.lines == null || slide.lines.Count == 0)
        {
            yield return HandleSlideEvents(slide);

            if (delayBetweenSlides > 0f)
                yield return new WaitForSecondsRealtime(delayBetweenSlides);

            yield break;
        }

        for (int i = 0; i < slide.lines.Count; i++)
        {
            if (keepLinesOnScreen)
                yield return TypeAppendLineRoutine(slide.lines[i]);
            else
                yield return TypeLineRoutine(slide.lines[i]);

            if (holdInputAfterAction)
                yield return LockInputRoutine(inputBlockTime);

            waitingForInput = true;
            yield return new WaitUntil(() => waitingForInput == false);

            if (delayBetweenLines > 0f && i < slide.lines.Count - 1)
                yield return new WaitForSecondsRealtime(delayBetweenLines);
        }

        yield return HandleSlideEvents(slide);

        if (holdInputAfterAction)
            yield return LockInputRoutine(inputBlockTime);

        if (delayBetweenSlides > 0f)
            yield return new WaitForSecondsRealtime(delayBetweenSlides);
    }

    private IEnumerator HandleSlideEvents(CinematicSlideData slide)
    {
        if (slide.triggerCharacterSelection)
        {
            waitingForExternalContinue = true;
            onCharacterSelectionRequested?.Invoke();
            yield return new WaitUntil(() => waitingForExternalContinue == false);
        }

        if (slide.triggerCustomEvent && !string.IsNullOrWhiteSpace(slide.customEventId))
            onCustomEventTriggered?.Invoke(slide.customEventId);
    }

    private IEnumerator TypeLineRoutine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || activeText == null)
            yield break;

        isTyping = true;
        isFastForwarding = false;

        string trimmedLine = line.Trim();
        activeText.text = string.Empty;
        nextVoiceTime = Time.unscaledTime;

        for (int i = 0; i < trimmedLine.Length; i++)
        {
            char letter = trimmedLine[i];
            activeText.text += letter;

            TryPlayVoice(letter);

            float currentDelay = isFastForwarding
                ? Mathf.Max(typingSpeed / fastTypingMultiplier, minFastTypingDelay)
                : typingSpeed;

            yield return new WaitForSecondsRealtime(currentDelay);
        }

        activeText.text = trimmedLine;

        isTyping = false;
        isFastForwarding = false;
    }

    private IEnumerator TypeAppendLineRoutine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || activeText == null)
            yield break;

        isTyping = true;
        isFastForwarding = false;

        string trimmedLine = line.Trim();

        if (!string.IsNullOrEmpty(activeText.text))
            activeText.text += "\n\n";

        nextVoiceTime = Time.unscaledTime;

        for (int i = 0; i < trimmedLine.Length; i++)
        {
            char letter = trimmedLine[i];
            activeText.text += letter;

            TryPlayVoice(letter);

            float currentDelay = isFastForwarding
                ? Mathf.Max(typingSpeed / fastTypingMultiplier, minFastTypingDelay)
                : typingSpeed;

            yield return new WaitForSecondsRealtime(currentDelay);
        }

        isTyping = false;
        isFastForwarding = false;
    }

    private void ConfigureTextMode(bool letterMode)
    {
        keepLinesOnScreen = letterMode;

        HideAllTexts();

        activeText = letterMode ? letterText : centerText;

        if (activeText != null)
        {
            activeText.text = string.Empty;
            activeText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("CinematicTextController: Active text is not assigned.");
        }
    }

    private void HideAllTexts()
    {
        if (centerText != null)
        {
            centerText.text = string.Empty;
            centerText.gameObject.SetActive(false);
        }

        if (letterText != null)
        {
            letterText.text = string.Empty;
            letterText.gameObject.SetActive(false);
        }
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
            SoundEffectManager.PlayVoice(new AudioClipData(clip, voiceVolume), pitch);
        else
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, voiceVolume);

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