using Animancer;
using UnityEngine;

public abstract class FirstPersonActorState : BaseState
{
    protected readonly Actor actor;
    protected IAnimationFacade animation=>actor.firstPersonAnimationFacade;
    protected StateMachine stateMachine=>actor.firstPersonStateSystem.Machine;
    protected FirstPersonStateRegistry stateRegistry=>
        actor.firstPersonStateSystem.Registry;
    protected FirstPersonWeaponAnimations Animations=>
        actor.weaponEquipment?.CurrentDefinition?.animationConfig?.FirstPerson;

    public override float NormalizedTime=>
        animation?.CurrentNormalizedTime??0f;

    protected FirstPersonActorState(Actor actor)
    {
        this.actor=actor;
    }

    protected void Play(TransitionAsset transition)
    {
        if(transition!=null)
            animation?.PlayTransition(transition,AnimPlayOptions.Default);
    }

    protected void ApplyMoveParameter()
    {
        animation?.SetMixerParameter(GetLocalMoveParameter());
    }

    protected bool IsFullBodyState(ActorStateType expected)
    {
        return TryGetFullBodyState(out ActorStateType current)&&
               current==expected;
    }

    protected bool IsFullBodyState(params ActorStateType[] expected)
    {
        if(!TryGetFullBodyState(out ActorStateType current))return false;

        for(int i=0;i<expected.Length;i++)
        {
            if(current==expected[i])return true;
        }

        return false;
    }

    protected bool IsMoving
    {
        get
        {
            if(actor.simulation.WantMove)return true;

            return IsFullBodyState(
                ActorStateType.MoveStart,
                ActorStateType.MoveLoop,
                ActorStateType.MoveStop,
                ActorStateType.AimMove);
        }
    }

    protected bool IsAiming
    {
        get
        {
            if(actor.aimSystem?.IsAiming==true)return true;

            return IsFullBodyState(
                ActorStateType.AimIdle,
                ActorStateType.AimMove);
        }
    }

    private bool TryGetFullBodyState(out ActorStateType stateType)
    {
        stateType=default;
        return actor.actorStateSystem?.Machine.CurrentState is ActorBaseState state&&
               actor.actorStateSystem.Registry.TryGetStateType(
                   state,
                   out stateType);
    }

    private Vector2 GetLocalMoveParameter()
    {
        Vector3 localDirection=actor.player.InverseTransformDirection(
            actor.simulation.locomotionData.DesiredWorldMoveDirection);
        return new Vector2(localDirection.x,localDirection.z);
    }
}
