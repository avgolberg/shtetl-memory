using Unity.Cinemachine;
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

    [Header("Camera")]
    [SerializeField] private float cameraSize; //Orthographic size here; 0 keeps the game's default (interiors are drawn closer)

    static float defaultCameraSize;

    public string LocationID => locationID;
    public string LocationName => locationName;
    public PolygonCollider2D MapBoundary => mapBoundary;
    public AudioClip MusicClip => musicClip;
    public float MusicVolume => musicVolume;
    public float MusicFadeDuration => musicFadeDuration;
    public float CameraSize => cameraSize;

    //Confines the camera to this location and applies its zoom
    public void ApplyToCamera(CinemachineConfiner2D confiner)
    {
        if (confiner == null) return;

        confiner.BoundingShape2D = mapBoundary;
        var cam = confiner.GetComponent<CinemachineCamera>();
        if (cam != null)
        {
            if (defaultCameraSize <= 0f) defaultCameraSize = cam.Lens.OrthographicSize;
            cam.Lens.OrthographicSize = cameraSize > 0f ? cameraSize : defaultCameraSize;
            confiner.InvalidateLensCache();
        }
        confiner.InvalidateBoundingShapeCache();
    }
}
