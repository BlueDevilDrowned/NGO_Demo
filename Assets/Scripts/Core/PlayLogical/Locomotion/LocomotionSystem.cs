using System;
using UnityEngine;

public sealed class LocomotionSystem : IActorSystem
{
    private readonly Actor actor;
    private readonly LocomotionIntentProcessor processor=new();
    private readonly LocomotionReplication replication;
    private bool hasState;
    private LocomotionData lastState;

    public LocomotionSystem(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        replication=new LocomotionReplication(actor);
        actor.RegisterSystem(this);
    }

    public void ServerTick()
    {
        if(!actor.IsServer)return;

        LocomotionData next=processor.Process(
            in actor.simulation.inputData,
            actor.simulation.cameraData.ViewYaw,
            actor.rootPoseSystem.AuthoritativeForward);
        actor.simulation.locomotionData=next;

        if(hasState&&Approximately(in lastState,in next))return;

        hasState=true;
        lastState=next;
        replication.MarkAuthoritativeState(in next);
    }

    public void PresentationUpdate()
    {
        if(replication.TrySampleMotionState(
               actor.localTick,
               out LocomotionMotionSnapshot motion))
        {
            actor.simulation.locomotionData.DesiredWorldMoveDirection=
                motion.DesiredWorldMoveDirection;
            actor.simulation.locomotionData.DesiredLocalMoveAngle=
                motion.DesiredLocalMoveAngle;
        }

        replication.TryConsumeState(out _, out _);
    }

    public void Dispose()
    {
        replication.Dispose();
    }

    private static bool Approximately(
        in LocomotionData left,
        in LocomotionData right)
    {
        return left.stateType==right.stateType&&
               (left.DesiredWorldMoveDirection-
                right.DesiredWorldMoveDirection).sqrMagnitude<=0.000001f&&
               Mathf.Abs(left.DesiredLocalMoveAngle-
                         right.DesiredLocalMoveAngle)<=0.001f;
    }
}
