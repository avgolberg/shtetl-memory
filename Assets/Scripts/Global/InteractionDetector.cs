using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    private IInteractable interactableInRange = null;
    public GameObject interactionIcon;

    void Start()
    {
        interactionIcon.SetActive(false);
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        bool canInteract = interactableInRange != null && interactableInRange.CanInteract();

        interactionIcon.SetActive(canInteract);

        if (canInteract)
            interactableInRange.Interact();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable))
        {
            interactableInRange = interactable;
            interactionIcon.SetActive(interactableInRange.CanInteract());
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && interactable == interactableInRange)
        {
            interactableInRange = null;
            interactionIcon.SetActive(false);
        }
    }
}
