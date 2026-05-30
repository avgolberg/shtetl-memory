using UnityEngine;

public class InteractiveGrave : MonoBehaviour, IInteractable
{
    [Header("Grave Info")]
    [SerializeField] private Sprite graveSprite;
    [SerializeField] private string symbolTitle;

    [TextArea]
    [SerializeField] private string description;

    [Header("Letter")]
    [SerializeField] private bool hasLetter;
    private bool letterWasRead;
    [SerializeField] private CinematicTextController cinematicController;
    [SerializeField] private CinematicSequenceSO letterSequence;

    public void Interact()
    {
        GraveInfoUI.Instance.Show(
            graveSprite,
            symbolTitle,
            description,
            hasLetter && !letterWasRead ? ShowLetter : null
        );
    }

    private void ShowLetter()
    {
        letterWasRead = true;

        if (cinematicController == null || letterSequence == null)
        {
            Debug.LogError("[InteractiveGrave] Missing cinematic setup.", this);
            Time.timeScale = 1f;
            return;
        }

        Time.timeScale = 1f;
        cinematicController.Play(letterSequence, true);
    }

    public bool CanInteract()
    {
        return true;
    }
}