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
    bool canInteract = false;
    string unlockFlag;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        sr = GetComponent<SpriteRenderer>();
        unlockFlag = "transition:" + (string.IsNullOrEmpty(Name) ? GlobalHelper.GenerateUniqueID(gameObject) : Name);
        if (isReturnTransition)
        {
            ChangeSprite();
        }
    }

    private void Start()
    {
        //Saves are loaded in SaveController.Awake, which runs after this Awake
        if (!isReturnTransition && SaveController.Instance.HasFlag(unlockFlag))
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

        targetLocation.ApplyToCamera(confiner);

        player.transform.position = teleportTargetPosition.position;
        PlaceOnGround(player);

        ScreenFader.Instance.NotifyTargetWarped(player.transform, player.transform.position - teleportTargetPosition.position);
        ScreenFader.Instance.SnapCameraToTarget(player.transform);

        await ScreenFader.Instance.FadeIn();
        SaveController.Instance.SetCurrentLocation(targetLocation);
        PauseController.SetPause(false);
    }

    //Arrival points can be above the ground. The game is paused during the fade, which zeroes the player's
    //velocity every frame, so the player would sink slowly; put the feet straight on the ground instead.
    static void PlaceOnGround(GameObject player)
    {
        Collider2D body = player.GetComponent<Collider2D>();
        if (body == null) return;

        Physics2D.SyncTransforms();
        Bounds bounds = body.bounds;
        int mask = Physics2D.GetLayerCollisionMask(player.layer);
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(new Vector2(bounds.center.x, bounds.min.y + 0.1f), Vector2.down, 20f, mask))
        {
            if (hit.collider.isTrigger || hit.collider.attachedRigidbody == body.attachedRigidbody) continue;

            player.transform.position += Vector3.down * (bounds.min.y - hit.point.y - 0.02f);
            break;
        }

        Rigidbody2D rb = body.attachedRigidbody;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    //Opens the transition for good (quest reward); the unlock is kept in the save
    public void Unlock()
    {
        ChangeSprite();
        SaveController.Instance.SetFlag(unlockFlag);
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
