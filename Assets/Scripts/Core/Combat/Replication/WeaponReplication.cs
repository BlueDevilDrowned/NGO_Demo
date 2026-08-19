using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 缓存服务器待发送事件和客户端待消费事件。
/// </summary>
public sealed class WeaponReplication : IActorSystem
{
    private readonly Actor actor;
    private readonly WeaponReplicationChannel channel;
    /// <summary>
    /// 服务器事件
    /// </summary>
    private readonly Queue<ShotData> outgoingEvents=new();
    /// <summary>
    /// 客户端事件，处理弹道，名字特效，设计动画等
    /// </summary>
    private readonly Queue<ShotData> incomingEvents=new();
    private bool isDisposed;

    public WeaponReplication(Actor actor)
    {
        this.actor=actor;
        channel=new(actor,this);
        channel.Register();
    }
    /// <summary>
    /// 装入outgingEvents中
    /// </summary>
    /// <param name="shotEvent"></param>
    public void EnqueueAuthoritativeEvent(in ShotData shotEvent)
    {
        if(isDisposed||!actor.IsServer)return;

        outgoingEvents.Enqueue(shotEvent);
    }
    /// <summary>
    /// 把outgiongEvents处理成快照
    /// </summary>
    /// <param name="snapshot"></param>
    /// <returns></returns>
    internal bool TryBuildSnapshot(out WeaponSnapshot snapshot)
    {
        snapshot=default;
        if(outgoingEvents.Count==0)return false;

        int count=Mathf.Min(outgoingEvents.Count,WeaponSnapshot.MaxEvents);
        snapshot.EventCount=(byte)count;
        for(int i=0;i<count;i++)
        {
            ShotData shotEvent=outgoingEvents.Dequeue();
            snapshot.SetEvent(i,in shotEvent);
        }
        return true;
    }


    /// <summary>
    /// 装入incomingEvents中
    /// </summary>
    /// <param name="snapshot"></param>
    /// <returns></returns>
    internal bool TryReceiveSnapshot(in WeaponSnapshot snapshot)
    {
        if(snapshot.EventCount==0||
           snapshot.EventCount>WeaponSnapshot.MaxEvents)return false;

        for(int i=0;i<snapshot.EventCount;i++)
        {
            ShotData shotEvent=snapshot.GetEvent(i);
            if(!IsValid(in shotEvent))return false;
        }

        for(int i=0;i<snapshot.EventCount;i++)
            incomingEvents.Enqueue(snapshot.GetEvent(i));
        return true;
    }
    /// <summary>
    /// 消费incomingEvents，获取第一个event
    /// </summary>
    /// <param name="shotEvent"></param>
    /// <returns></returns>
    public bool TryConsumeEvent(out ShotData shotEvent)
    {
        shotEvent=default;
        if(incomingEvents.Count==0)return false;

        shotEvent=incomingEvents.Dequeue();
        return true;
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        channel.Unregister();
        outgoingEvents.Clear();
        incomingEvents.Clear();
    }

    private static bool IsValid(in ShotData shot)
    {
        bool isHit=shot.EventType==ShotEventType.Hit;
        return shot.Sequence>0&&
               shot.ProjectileId>0&&
               shot.WeaponId>0&&
               shot.EventType<=ShotEventType.Expired&&
               shot.HasHit==isHit&&
               (isHit?shot.HitLayer<=31:shot.HitLayer==byte.MaxValue)&&
               IsFinite(shot.TracerSpeed)&&shot.TracerSpeed>0f&&
               IsFinite(shot.Gravity)&&shot.Gravity>=0f&&
               IsFinite(shot.Range)&&shot.Range>0f&&
               IsFinite(shot.Origin)&&
               IsFinite(shot.EndPoint)&&
               IsFinite(shot.HitNormal);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&IsFinite(value.z);
    }
}
