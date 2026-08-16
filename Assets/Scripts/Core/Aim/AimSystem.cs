using System;


/// <summary>
/// 主要负责表现层，目前准备各个系统同时维护表现层与权威层，表现层数据经过权威层仲裁后返回表现层
/// </summary>
public class AimSystem:IActorSystem
{
    public Actor actor;
    public AimChannel channel;
    public AimSystem(Actor actor)
    {
        this.actor=actor;
        OnAimChange+=OnAimChanged;
        PreAimState=false;
        channel=new(actor);
        actor.RegisterSystem(this);
    }

    /// <summary>
    /// 表现层数据，客户端维护，服务器仲裁
    /// </summary>
    public AimData data;
    public bool IsAiming=>data.IsAiming;
    public Action OnAimChange;
    /// <summary>
    /// 客户端修改aim再由服务器决定是否接收，纠正状态
    /// </summary>
    /// <param name="ifAim"></param>
    public void SetPresentationAim(bool ifAim)
    {
        //只允许本机预测
        if(!actor.IsOwner)return;
        data.IsAiming=ifAim;
    }
    /// <summary>
    /// 有两种可能，客户端自己切换aim，服务器同步权威数据导致切换
    /// </summary>
    public void OnAimChanged()
    {
        if(data.IsAiming)
        {
            //瞄准状态
            //状态机切换瞄准状态机，摄像机切换瞄准模式

            //摄像机部分已由摄像机维护
        }
    }
    private bool PreAimState=false;
    public void PresentationUpdate()
    {
        if(!actor.IsOwner)return;
        if(PreAimState!=data.IsAiming)
        {
            OnAimChange?.Invoke();
        }
    }
    public bool isDisposed;
    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
    }

}