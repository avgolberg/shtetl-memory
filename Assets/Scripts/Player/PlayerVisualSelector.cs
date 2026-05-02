using UnityEngine;

public class PlayerVisualSelector : MonoBehaviour
{
    private Animator animator;

    [Header("Controllers")]
    private SpriteRenderer spriteRenderer;
    [SerializeField] private RuntimeAnimatorController girlController;
    [SerializeField] private RuntimeAnimatorController boyController;

    [Header("Start Sprites")]
    [SerializeField] private Sprite girlIdleSprite;
    [SerializeField] private Sprite boyIdleSprite;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplySelectedCharacter();
    }

    public void ApplySelectedCharacter()
    {
        if (animator == null)
            return;

        bool isBoy = PlayerCharacterState.SelectedCharacter == PlayerCharacterType.Boy;

        animator.runtimeAnimatorController = isBoy ? boyController : girlController;

        if (spriteRenderer != null)
            spriteRenderer.sprite = isBoy ? boyIdleSprite : girlIdleSprite;

        animator.Rebind();
        animator.Update(0f);
    }
}