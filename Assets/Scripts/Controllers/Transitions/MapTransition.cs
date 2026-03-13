using Unity.Cinemachine;
using UnityEngine;

public class MapTransition : MonoBehaviour, IInteractable
{
    public int ID;
    public string Name;
    public Sprite updatedSprite;
    [SerializeField] private Location targetLocation;
    [SerializeField] Transform teleportTargetPosition;
    [SerializeField] bool isReturnTransition;

    CinemachineConfiner2D confiner;
    SpriteRenderer sr;
    GameObject player;
    SaveController saveController;
    bool canInteract = false;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        sr = GetComponent<SpriteRenderer>();
        saveController = FindFirstObjectByType<SaveController>();
        if (isReturnTransition)
        {
            ChangeSprite();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        player = collision.gameObject;
    }

    async void FadeTransition(GameObject player)
    {
        PauseController.SetPause(true);

        await ScreenFader.Instance.FadeOut();
        SoundEffectManager.PlayMusic(targetLocation.MusicClip, targetLocation.MusicVolume, targetLocation.MusicFadeDuration);

        confiner.BoundingShape2D = targetLocation.MapBoundary;
        confiner.InvalidateBoundingShapeCache();

        player.transform.position = teleportTargetPosition.position;

        ScreenFader.Instance.NotifyTargetWarped(player.transform, player.transform.position - teleportTargetPosition.position);
        ScreenFader.Instance.SnapCameraToTarget(player.transform);

        await ScreenFader.Instance.FadeIn();
        saveController.SetCurrentLocation(targetLocation);
        PauseController.SetPause(false);
    }

    public void ChangeSprite()
    {
        sr.sprite = updatedSprite;
        canInteract = true;
    }

    public void Interact()
    {
        if (player == null) return;
        if (!CanInteract()) return;
        
        FadeTransition(player);
    }

    public bool CanInteract()
    {
        return canInteract;
    }
}
