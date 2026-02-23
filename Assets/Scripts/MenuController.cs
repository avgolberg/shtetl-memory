using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject menuCanvas;

    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        menuCanvas.SetActive(!menuCanvas.activeSelf);
        if (menuCanvas.activeSelf) SoundEffectManager.Play("MenuClose");
        else SoundEffectManager.Play("MenuOpen");
    }
}
