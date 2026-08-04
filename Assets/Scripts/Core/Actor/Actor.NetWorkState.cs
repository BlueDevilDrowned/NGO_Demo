using Unity.Netcode;
using UnityEngine;

public partial class Actor
{
    //服务端可写，所有人可读
    private NetworkVariable<ActorStateType>networkState=new(
        ActorStateType.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    //服务端发送切换
    private void PublishCurrentState()
    {
        if(!IsServer)return;

        ActorBaseState currentState=stateMachine.CurrentState as ActorBaseState;

        if(currentState==null)return;

        if(networkState.Value==currentState.StateType)return;

        networkState.Value=currentState.StateType;
    }
    private void RegisterNetworkState()
    {
        networkState.OnValueChanged+=OnNetworkStateChanged;

        if(IsServer)
        {
            PublishCurrentState();
            return;
        }

        ApplyNetworkState(networkState.Value);
    }
    private void OnNetworkStateChanged(ActorStateType previousState,ActorStateType newState)
    {
        if(IsServer)return;

        ApplyNetworkState(newState);
    }
    private void ApplyNetworkState(ActorStateType stateType)
    {
        ActorBaseState targetState=StateRegistry.GetState(stateType);

        if(targetState==null)return;

        if(ReferenceEquals(stateMachine.CurrentState,targetState))
        {
            return;
        }

        stateMachine.ChangeState(targetState);
    }
    private void UnregisterNetworkState()
    {
        networkState.OnValueChanged -= OnNetworkStateChanged;
    }
}
