using System.Collections.Generic;

public class ActorMovement
{
    public Actor actor;
    public MovementResolver resolver;//仲裁
    public MovementMotor motor;//执行
    public GraviteModule gravite;

   
    public ActorMovement(Actor actor)
    {
        this.actor=actor;
        resolver=new();
        motor=new(actor);
        gravite=new(actor);
    }
    public void BeginTick()
    {
        gravite.BeginTick();
    }
    public void Execute()
    {
        // 1. 先处理会跨 Tick 保留的速度状态。
        gravite.verticalVelocity=resolver.ResolveVerticalVelocity(
            requests,
            gravite.verticalVelocity);

        // 2. 基于仲裁后的速度进行接地处理和重力积分。
        gravite.GraviteTick();

        // 3. 将最终速度与本 Tick 的位移、旋转请求合成。
        MovementResult result=resolver.ResolveMotion(
            requests,
            gravite.verticalVelocity,
            TickTime.deltaTime);

        // 4. 每个 Tick 只执行一次实际移动。
        motor.Execute(result);
        requests.Clear();
    }

    private readonly List<MovementRequest>requests=new();

    public void Submit(in MovementRequest request)
    {
        requests.Add(request);
    }
}
