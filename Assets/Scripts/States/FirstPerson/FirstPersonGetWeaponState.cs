public sealed class FirstPersonGetWeaponState : FirstPersonActorState
{
    public FirstPersonGetWeaponState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        var equip=Animations?.Equipment?.Equip;
        if(equip==null)
        {
            Complete();
            return;
        }
        Play(equip);
        stateMachine.SetOnEndCallback(Complete);
    }

    private void Complete()
    {
        FirstPersonActorState idle=stateRegistry.GetState(
            FirstPersonStateType.Idle);
        stateMachine.ChangeState(idle);
    }
}
