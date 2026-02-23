using UnityEngine;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    private static SoundEffectManager Instance;
    private static AudioSource audioSource;
     private static AudioSource randomPitchAudioSource;
    private static AudioSource voiceAudioSource;
    private static SoundEffectLibrary soundEffectLibrary;
    private static float sfxVolume = 1f;
    private static float musicVolume = 1f;
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
            soundEffectLibrary = GetComponent<SoundEffectLibrary>();
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        AudioClipData clipData = soundEffectLibrary.GetRandomClip(soundName);
        if(clipData != null)
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

    void Start()
    {
        sfxSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
        musicSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    public void OnValueChanged()
    {
        sfxVolume = sfxSlider.value;
        musicVolume = musicSlider.value;
    }
}
