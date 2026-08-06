using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

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
        gravite=new(actor,this);
    }
    public void Execute()
    {
        //tick更新
        //下面可以处理重力什么的速度维护，然后经过resolver审批,再commit

        //重力更新，再提交速度作用到位移
        gravite.GraviteTick();
        gravite.GraviteSumbit();
        
        Commit();
    }

    //
    private readonly List<MovementRequest>requests=new();

    public void Submit(in MovementRequest request)
    {
        requests.Add(request);
    }
    
    public void Commit()
    {
        MovementResult result=resolver.Resolve(requests);
        motor.Execute(result);
    }

}
