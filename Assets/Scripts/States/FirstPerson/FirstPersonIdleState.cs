using UnityEngine;

public sealed class FirstPersonIdleState : FirstPersonActorState
{
    public FirstPersonIdleState(Actor actor) : base(actor)
    {
    }

    public override void PresentationUpdate(float deltaTime)
    {
        if(IsAiming)
        {
            TransitionTo(FirstPersonStateType.AimIdle);
            return;
        }

        if(IsMoving)
        {
            TransitionTo(FirstPersonStateType.Move);
            return;
        }

        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
        }
    }


    public override void Enter()
    {
        Play(Animations?.Idle);
    }
}
