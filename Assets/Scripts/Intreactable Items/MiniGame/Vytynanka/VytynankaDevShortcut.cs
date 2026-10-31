using UnityEngine;
using UnityEngine.InputSystem;

//Development only (editor and development builds): F9 opens the Vytynanka mini-game anywhere, Shift+F9 resets it.
//A temporary test entry until the House of Culture workshop table exists. Sits on an always-active object (MiniGameRegistry).
public class VytynankaDevShortcut : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame)
            return;

        VytynankaMiniGame game = VytynankaMiniGame.Instance;
        if (game == null)
        {
            Debug.LogWarning("Vytynanka mini-game not found in the scene.");
            return;
        }

        if (Keyboard.current.shiftKey.isPressed)
            game.DevReset();
        else if (!game.IsOpen && !PauseController.IsGamePaused)
            game.Open(null);
    }
#endif
}
