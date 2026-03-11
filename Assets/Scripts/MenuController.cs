using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject menuCanvas;

    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        menuCanvas.SetActive(!menuCanvas.activeSelf);
        SoundEffectManager.Play("MenuToggle");
    }
}
