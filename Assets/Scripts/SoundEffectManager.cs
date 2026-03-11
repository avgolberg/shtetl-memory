using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    private static SoundEffectManager Instance;
    private static AudioSource audioSource;
    private static AudioSource randomPitchAudioSource;
    private static AudioSource voiceAudioSource;
    private static AudioSource musicAudioSource;
    private Coroutine musicCoroutine;
    private static SoundEffectLibrary soundEffectLibrary;
    private static float sfxVolume = 1f;
    private static float musicVolume = 1f;
    private static float musicTargetVolume = 1f;
    public static float SFXVolume => sfxVolume;
    public static float MusicVolume => musicVolume;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider musicSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            AudioSource[] audioSources = GetComponents<AudioSource>();
            audioSource = audioSources[0];
            randomPitchAudioSource = audioSources[1];
            voiceAudioSource = audioSources[2];
            musicAudioSource = audioSources[3];
            soundEffectLibrary = GetComponent<SoundEffectLibrary>();
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        sfxSlider.value = sfxVolume;
        musicSlider.value = musicVolume;

        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
    }

    private void OnSFXSliderChanged(float value)
    {
        sfxVolume = sfxSlider.value;
    }

    private void OnMusicSliderChanged(float value)
    {
        musicVolume = musicSlider.value;
        SetMusicVolume();
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        AudioClipData clipData = soundEffectLibrary.GetRandomClip(soundName);
        if (clipData != null)
        {
            if (randomPitch)
            {
                randomPitchAudioSource.pitch = Random.Range(1f, 1.5f);
                randomPitchAudioSource.PlayOneShot(clipData.clip, clipData.volume * sfxVolume);
            }
            else
            {
                audioSource.PlayOneShot(clipData.clip, clipData.volume * sfxVolume);
            }
        }
    }

    public static void PlayVoice(AudioClipData clipData, float pitch = 1f)
    {
        voiceAudioSource.pitch = pitch;
        voiceAudioSource.volume = clipData.volume * sfxVolume;
        voiceAudioSource.clip = clipData.clip;
        voiceAudioSource.Stop();
        voiceAudioSource.Play();
    }

    public static void StopVoice()
    {
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();
    }

    public static void PlayMusic(AudioClip clip, float targetVolume = 0.5f, float fadeDuration = 1f)
    {
        if (clip == null || musicAudioSource == null || Instance == null)
            return;

        if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
            return;

        if (Instance.musicCoroutine != null)
            Instance.StopCoroutine(Instance.musicCoroutine);

        musicTargetVolume = targetVolume;

        Instance.musicCoroutine = Instance.StartCoroutine(
            Instance.CrossfadeMusicRoutine(clip, targetVolume, fadeDuration)
        );
    }
    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float targetVolume, float fadeDuration)
    {
        if (musicAudioSource.isPlaying && musicAudioSource.clip != null)
        {
            float startVolume = musicAudioSource.volume;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                yield return null;
            }

            musicAudioSource.Stop();
        }

        musicAudioSource.clip = newClip;
        musicAudioSource.volume = 0f;
        musicAudioSource.loop = true;
        musicAudioSource.Play();

        float fadeInTimer = 0f;
        float finalVolume = targetVolume * musicVolume;

        while (fadeInTimer < fadeDuration)
        {
            fadeInTimer += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(0f, finalVolume, fadeInTimer / fadeDuration);
            yield return null;
        }

        musicAudioSource.volume = finalVolume;
        musicCoroutine = null;
    }

    public static void SetMusicVolume()
    {
        if (musicAudioSource != null)
            musicAudioSource.volume = musicTargetVolume * musicVolume;
    }
    
    public static void InitializeVolumes(float loadedSfx, float loadedMusic)
    {
        sfxVolume = loadedSfx;
        musicVolume = loadedMusic;
        SetMusicVolume();
    }
}
