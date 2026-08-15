public class AimSystem
{
    public Actor actor;
    public AimSystem(Actor actor)
    {
        this.actor=actor;
    }

    //
    public AimData data;
    //客户端修改aim再由服务器决定是否接收，纠正状态
    public void SetPresentationAim(bool ifAim)
    {
        //只允许本机预测
        if(!actor.IsOwner)return;
        data.IsAiming=ifAim;
    }
}