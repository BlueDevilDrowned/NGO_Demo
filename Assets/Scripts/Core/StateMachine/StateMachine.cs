public class StateMachine
{
    public BaseState CurrentState;
    public void Initialize(BaseState startState)
    {
        CurrentState=startState;
        CurrentState.Enter();
    }
    public void ChangeState(BaseState newState)
    {
        CurrentState.Exit();
        CurrentState=newState;
        CurrentState.Enter();
    }
}
