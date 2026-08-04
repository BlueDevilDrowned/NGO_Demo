using Animancer.FSM;
using Unity.Netcode;
using UnityEngine;
//状态机同步组件
public partial class Actor
{
    //服务端可写，所有人可读
    private NetworkVariable<ActorStateSnapshot>networkSnapshot=new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private ActorStateType lastServerState;
    private uint stateEnterTick;
    private bool hasPublishedState;
    //服务端发送切换
    private void PublishCurrentSnapshot()
    {
        if(!IsServer)return;

        ActorBaseState currentState=stateMachine.CurrentState as ActorBaseState;

        if(currentState==null)return;

        ActorStateType currentStateType=StateRegistry.GetStateType(currentState);
        //进入新的状态，重置状态开始Tick
        if(!hasPublishedState||currentStateType!=lastServerState)
        {
            hasPublishedState=true;
            lastServerState=currentStateType;

            stateEnterTick=(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick;
        }
        //传输状态所需数据
        ActorStateSnapshot snapshot=new()
        {
            StateType=currentStateType,
            StateEnterTick=stateEnterTick,
            input=runTimeData.Input,
            blackboard=runTimeData.blackboard,
        };

        networkSnapshot.Value=snapshot;

        
    }
    private void RegisterNetworkState()
    {
        networkSnapshot.OnValueChanged+=OnNetworkSnapshotChanged;

        if(IsServer)
        {
            PublishCurrentSnapshot();
            return;
        }

        ApplySnapshot(networkSnapshot.Value);
    }
    private void OnNetworkSnapshotChanged(ActorStateSnapshot previousSnapshot,ActorStateSnapshot newSnapshot)
    {
        if(IsServer)return;

        ApplySnapshot(newSnapshot);
    }
    private void ApplySnapshot(ActorStateSnapshot snapshot)
    {
        runTimeData.Input=snapshot.input;
        runTimeData.blackboard=snapshot.blackboard;

        ApplyNetworkState(snapshot.StateType);
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
        networkSnapshot.OnValueChanged -= OnNetworkSnapshotChanged;
    }
}
