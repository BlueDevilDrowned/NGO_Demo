using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

/**
 * 封装了一个处理角色受击反应的系统类
 * 实现了IProjectileHitReceiver接口，用于接收和处理投射物击中事件
 */
public sealed class HitReactionSystem : IProjectileHitReceiver
{

    // 定义常量：层索引和最大最近事件数量
    private const int Layer=2;  // 动画层索引
    private const int MaxRecentEvents=HitReactionSnapshot.MaxEvents;  // 最大记录的最近事件数



    // 系统依赖的组件和字段
    private readonly Actor actor;  // 角色对象
    private readonly IAnimationFacade animation;  // 动画外观接口
    private readonly AnimancerData animationData;  // 动画数据
    private readonly List<HitReactionEvent> recentEvents=new(MaxRecentEvents);  // 存储最近的受击事件列表
    private readonly Queue<HitReactionEvent> presentationQueue=new();  // 待展示的受击事件队列
    private uint eventSequence;  // 事件序列号
    private uint lastPresentedSequence;  // 最后展示的事件序列号

    public HitReactionSystem(
        Actor actor,
        IAnimationFacade animation,
        AnimancerData animationData)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        this.animation=animation??throw new ArgumentNullException(nameof(animation));
        this.animationData=animationData;
    }

    public void ReceiveProjectileHit(in ProjectileHitResult hit)
    {
        if(!actor.IsServer||hit.Target!=actor)return;

        eventSequence++;
        HitReactionEvent reaction=new()
        {
            Sequence=eventSequence,
            Tick=TickTime.CurrentServerTick,
            Location=hit.Location,
            Direction=hit.Direction,
            WeaponType=hit.WeaponType,
            Damage=Mathf.Max(0f,hit.Damage),
        };
        if(recentEvents.Count==MaxRecentEvents)
            recentEvents.RemoveAt(0);
        recentEvents.Add(reaction);

        if(actor.IsClient)
            QueueForPresentation(in reaction);
    }

    internal void CopyRecentEvents(ref HitReactionSnapshot snapshot)
    {
        int count=Math.Min(recentEvents.Count,MaxRecentEvents);
        snapshot.EventCount=(byte)count;
        int start=recentEvents.Count-count;
        for(int i=0;i<count;i++)
        {
            HitReactionEvent reaction=recentEvents[start+i];
            snapshot.SetEvent(i,in reaction);
        }
    }

    internal void ApplyAuthoritativeEvent(in HitReactionEvent reaction)
    {
        QueueForPresentation(in reaction);
    }

    public void PresentationUpdate()
    {
        if(presentationQueue.Count==0)return;

        HitReactionEvent reaction=presentationQueue.Dequeue();
        while(presentationQueue.Count>0)
            reaction=presentationQueue.Dequeue();

        Play(in reaction);
    }

    private void QueueForPresentation(in HitReactionEvent reaction)
    {
        if(reaction.Sequence<=lastPresentedSequence)return;

        lastPresentedSequence=reaction.Sequence;
        presentationQueue.Enqueue(reaction);
    }

    private void Play(in HitReactionEvent reaction)
    {
        if(animationData==null)return;

        HitReactionDirection direction=ResolveDirection(reaction.Direction);
        TransitionAsset transition=animationData.HitReaction.Get(
            reaction.Location,
            direction);
        if(transition==null)return;

        float fadeIn=Mathf.Max(0f,animationData.HitReaction.FadeInDuration);
        float fadeOut=Mathf.Max(0f,animationData.HitReaction.FadeOutDuration);
        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=Layer;
        options.FadeDuration=fadeIn;
        animation.PlayTransition(transition,options);
        animation.SetLayerWeight(Layer,1f,fadeIn);
        animation.SetOnEndCallback(
            ()=>animation.SetLayerWeight(Layer,0f,fadeOut),
            Layer);
    }

    private HitReactionDirection ResolveDirection(Vector3 projectileDirection)
    {
        Vector3 sourceDirection=-projectileDirection;
        Vector3 local=actor.transform.InverseTransformDirection(sourceDirection);
        if(Mathf.Abs(local.z)>=Mathf.Abs(local.x))
            return local.z>=0f
                ?HitReactionDirection.Front
                :HitReactionDirection.Back;

        return local.x>=0f
            ?HitReactionDirection.Right
            :HitReactionDirection.Left;
    }
}
