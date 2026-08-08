public class UpperBodyStateMachine
{
    public UpperBodyState CurrentState{get;private set;}

    public void Initialize(UpperBodyState startState)
    {
        if(startState==null)
            throw new System.ArgumentNullException(nameof(startState));

        CurrentState=startState;
        CurrentState.Enter();
    }

    public void ChangeState(UpperBodyState next)
    {
        if(next==null||ReferenceEquals(CurrentState,next))return;
        CurrentState?.Exit();
        CurrentState=next;
        CurrentState.Enter();
    }
}
