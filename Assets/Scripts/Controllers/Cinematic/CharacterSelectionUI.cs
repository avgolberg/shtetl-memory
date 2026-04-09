using UnityEngine;

public class CharacterSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private CinematicTextController cinematicController;

    public void Show()
    {
        if (cinematicController != null)
            cinematicController.SetCinematicPanelRaycastState(false);

        panel.SetActive(true);
    }

    public void SelectBoy()
    {
        ApplySelection(PlayerCharacterType.Boy);
    }

    public void SelectGirl()
    {
        ApplySelection(PlayerCharacterType.Girl);
    }

    private void ApplySelection(PlayerCharacterType type)
    {
        print(type);
        PlayerCharacterState.SelectedCharacter = type;

        panel.SetActive(false);

        if (cinematicController != null)
        {
            cinematicController.SetCinematicPanelRaycastState(true);
            cinematicController.ResumeAfterExternalAction();
        }
    }
}

public enum PlayerCharacterType
{
    Boy, Girl
}

public static class PlayerCharacterState
{
    public static PlayerCharacterType SelectedCharacter = PlayerCharacterType.Girl;
}