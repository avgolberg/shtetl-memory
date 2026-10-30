using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    //Everything in range; E uses the one nearest to the player (horizontally), so overlapping zones stay unambiguous
    private readonly List<IInteractable> interactablesInRange = new();
    public GameObject interactionIcon;

    private IInteractable InteractableInRange
    {
        get
        {
            interactablesInRange.RemoveAll(i => i as Object == null);

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (IInteractable interactable in interactablesInRange)
            {
                float distance = Mathf.Abs(((Component)interactable).transform.position.x - transform.position.x);
                if (distance < nearestDistance)
                {
                    nearest = interactable;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
    }

    //The nearest object changes as the player walks, so keep the icon in step with it
    void Update()
    {
        if (interactablesInRange.Count == 0) return;

        IInteractable interactable = InteractableInRange;
        interactionIcon.SetActive(interactable != null && interactable.CanInteract());
    }

    void Start()
    {
        interactionIcon.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        //Interact uses a Hold interaction: a tap sends started and canceled, so react only to the press
        if (!context.started) return;

        IInteractable interactable = InteractableInRange;
        bool canInteract = interactable != null && interactable.CanInteract();

        interactionIcon.SetActive(canInteract);

        if (canInteract)
            interactable.Interact();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && interactablesInRange.Remove(interactable)
            && interactablesInRange.Count == 0)
        {
            interactionIcon.SetActive(false);
        }
    }
}
