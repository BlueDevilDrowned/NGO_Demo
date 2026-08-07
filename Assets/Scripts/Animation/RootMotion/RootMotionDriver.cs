using UnityEngine;

public class RootMotionDriver
{
    private Actor actor;
    public RootMotionDriver(Actor actor)
    {
        this.actor=actor;
    }
    //
    //计算并提交
    //
    public void SubmitClipMotion(RootMotionData data,IAnimationFacade animation)
    {
        if(data==null||animation==null)return;

        RootMotionDelate delate=CaulateClipMotion(data,animation);
        //提交数据请求
        MovementRequest request=MovementRequest.Default;
        request.Source="RootMotionDriver";
        request.WorldPositionDelta=delate.WorldPositionDelta;
        request.ForwardPositionDelta=0f;
        request.YawDelta=delate.WorldYawDelta;

        actor.movement.Submit(request);
    }
    public RootMotionDelate CaulateClipMotion(RootMotionData data,IAnimationFacade animation)
    {
        Vector3 positionDelta=CaulateClipMotionPosition(data,animation);
        float YawDelta=CaulateClipMotionRotation(data,animation);
        return new RootMotionDelate
        {
            WorldPositionDelta=positionDelta,
            WorldYawDelta=YawDelta,
        };
    }
    public Vector3 CaulateClipMotionPosition(RootMotionData data,IAnimationFacade animation)
    {
        Vector3 positionDelta=Vector3.zero;
        //由于动画是局部速度，所以变一下
        //z向前，x右，y上
        RootMotionSample sample=data.Evaluate(animation.CurrentNormalizedTime);
        Vector3 velocity=sample.LocalVelocity;
        positionDelta+=velocity.x*actor.player.right*TickTime.deltaTime;
        positionDelta+=velocity.z*actor.player.forward*TickTime.deltaTime;
        positionDelta+=velocity.y*actor.player.up*TickTime.deltaTime;
        return positionDelta;
    }
    public float CaulateClipMotionRotation(RootMotionData data,IAnimationFacade animation)
    {
        RootMotionSample sample=data.Evaluate(animation.CurrentNormalizedTime);
        //顺时针是正
        float YawDelta=sample.AngularVelocityY*TickTime.deltaTime;
        return YawDelta;
    }
}
