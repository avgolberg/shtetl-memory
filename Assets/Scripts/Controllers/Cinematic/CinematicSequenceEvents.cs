using UnityEngine;
using System.Collections;

public class CinematicSequenceEvents : MonoBehaviour
{
    [SerializeField] private HiddenObjectGameController boxInteraction;
    [SerializeField] private CinematicTextController cinematicController;
    [SerializeField] private CanvasGroup cinematicRootCanvasGroup;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private Quest quest;

    public void HandleCustomEvent(string eventId)
    {
        switch (eventId)
        {
            case "BoxInteraction":
                boxInteraction.OnCompleted += HandleBoxMiniGameCompleted;
                StartCoroutine(OpenMemoryBoxRoutine());
                break;
        }
    }

    private void HandleBoxMiniGameCompleted()
    {
        boxInteraction.OnCompleted -= HandleBoxMiniGameCompleted;
        QuestController.Instance.AcceptQuest(quest);

        SaveController.Instance.MarkIntroSeen();
        
        if (cinematicController != null)
            cinematicController.ResumeAfterExternalAction();
    }

    private IEnumerator OpenMemoryBoxRoutine()
    {
        yield return FadeCanvasGroup(cinematicRootCanvasGroup, 1f, 0f, fadeDuration);

        if (cinematicController != null)
            cinematicController.SetCinematicPanelRaycastState(false);

        boxInteraction.Open();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        float time = 0f;
        cg.alpha = from;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        cg.alpha = to;
    }    
}