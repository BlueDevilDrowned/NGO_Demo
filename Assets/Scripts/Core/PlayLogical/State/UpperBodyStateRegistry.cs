using System.Collections.Generic;
using UnityEngine;

public sealed class UpperBodyStateRegistry
{
    private readonly Dictionary<UpperBodyStateType,UpperBodyState>states=new();

    public UpperBodyState InitialState=>GetState(UpperBodyStateType.Idle);

    public void Initialize(Actor actor)
    {
        if(actor==null)
            throw new System.ArgumentNullException(nameof(actor));

        Register(new UpperBodyIdleState(actor));
        Register(new UpperBodyGetWeaponState(actor));
        Register(new UpperBodyChangeClipState(actor));
        Register(new UpperBodyProneIdleState(actor));
        Register(new UpperBodyProneGetWeaponState(actor));
        Register(new UpperBodyProneChangeClipState(actor));
    }

    public UpperBodyState GetState(UpperBodyStateType stateType)
    {
        if(states.TryGetValue(stateType,out UpperBodyState state))
            return state;

        Debug.LogError($"Upper-body state is not registered: {stateType}");
        return null;
    }

    public bool TryGetState(
        UpperBodyStateType stateType,
        out UpperBodyState state)
    {
        return states.TryGetValue(stateType,out state);
    }

    private void Register(UpperBodyState state)
    {
        states.Add(state.StateType,state);
    }
}
