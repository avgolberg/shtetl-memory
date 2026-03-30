using UnityEngine;

public class NPCAnimationController : MonoBehaviour
{
    public enum AnimState
    {
        Idle = 0, Walk = 1, Run = 2, Dizzy = 3, Sit = 4, SitSwing = 5, IdleCasC = 6
    }

    private Animator animator;
    [SerializeField] private AnimState defaultIdleState = AnimState.Idle;
    [SerializeField] private AnimState moveState = AnimState.Walk;

    private static readonly int AnimStateHash = Animator.StringToHash("animState");

    public AnimState DefaultIdleState => defaultIdleState;
    public AnimState MoveState => moveState;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void SetState(AnimState state)
    {
        animator.SetInteger(AnimStateHash, (int)state);
    }

    public void SetIdle()
    {
        SetState(defaultIdleState);
    }

    public void SetMove()
    {
        SetState(moveState);
    }
}