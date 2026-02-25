using Unity.Cinemachine;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundary;
    [SerializeField] Transform teleportTargetPosition;
    private CinemachineConfiner2D confiner;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        FadeTransition(collision.gameObject);

        //MapController_Manual.Instance?.HighlightArea(mapBoundry.name);
        //MapController_Dynamic.Instance?.UpdateCurrentArea(mapBoundry.name);
    }
    
     async void FadeTransition(GameObject player)
    {
        PauseController.SetPause(true);

        await ScreenFader.Instance.FadeOut();

        confiner.BoundingShape2D = mapBoundary;
        confiner.InvalidateBoundingShapeCache();

        player.transform.position = teleportTargetPosition.position;

        ScreenFader.Instance.NotifyTargetWarped(player.transform, player.transform.position - teleportTargetPosition.position);
        ScreenFader.Instance.SnapCameraToTarget(player.transform);

        await ScreenFader.Instance.FadeIn();

        PauseController.SetPause(false);
    }
}
