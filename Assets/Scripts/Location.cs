using UnityEngine;

[System.Serializable]
public class Location : MonoBehaviour
{
    [SerializeField] private string locationID;
    [SerializeField] private string locationName;
    [SerializeField] private PolygonCollider2D mapBoundary;

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float musicVolume = 0.45f;
    [SerializeField] private float musicFadeDuration = 1f;

    public string LocationID => locationID;
    public string LocationName => locationName;
    public PolygonCollider2D MapBoundary => mapBoundary;
    public AudioClip MusicClip => musicClip;
    public float MusicVolume => musicVolume;
    public float MusicFadeDuration => musicFadeDuration;
}
