using UnityEngine;

public sealed class FirstPersonIdleState : FirstPersonActorState
{
    public FirstPersonIdleState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return !IsAiming&&!IsMoving&&!IsFullBodyState(
            ActorStateType.Jump,
            ActorStateType.Fall,
            ActorStateType.Land);
    }

    public override void Enter()
    {
        Play(Animations?.Idle);
    }
}
