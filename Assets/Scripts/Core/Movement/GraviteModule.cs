using UnityEngine;
using System;

public class GraviteModule
{
    private Actor actor;
    private bool hasGroundSample;
    private bool wasGrounded;

    public float verticalVelocity;
    private float CurrentGravite;
    public bool IsGrounded{get;private set;}
    public bool JustLanded{get;private set;}
    public float LastImpactVelocityY{get;private set;}
    public float LastImpactSpeed=>Mathf.Max(0f,-LastImpactVelocityY);
    public GraviteModule(Actor actor)
    {
        this.actor=actor!=null
            ?actor
            :throw new ArgumentNullException(nameof(actor));
    }

    public void BeginTick()
    {
        bool grounded=
            actor.characterController.isGrounded&&
            verticalVelocity<=0f;

        JustLanded=hasGroundSample&&grounded&&!wasGrounded;
        if(JustLanded)
            LastImpactVelocityY=verticalVelocity;

        hasGroundSample=true;
        wasGrounded=grounded;
        IsGrounded=grounded;

        if(IsGrounded)
            verticalVelocity=actor.controllerSO.GroundedVelocity;
    }

    public void GraviteTick()
    {
        CurrentGravite=actor.controllerSO.Gravite;
        // 起跳位移发生前 CharacterController 仍会报告接地，不能覆盖向上速度。
        if(IsGrounded&&verticalVelocity<=0f)
        {
            verticalVelocity=actor.controllerSO.GroundedVelocity;
            return;
        }
        //否则按速度更新
        if(verticalVelocity>0)CurrentGravite=actor.controllerSO.Gravite*actor.controllerSO.UpFactor;
        else if(verticalVelocity<0)
        {
            CurrentGravite=actor.controllerSO.Gravite*actor.controllerSO.FallFactor;
        }
        //再额外判断是否处于最高点
        if(verticalVelocity>-actor.controllerSO.HoldSpeed&&verticalVelocity<actor.controllerSO.HoldSpeed)CurrentGravite=actor.controllerSO.Gravite*actor.controllerSO.HoldFactor;
        

        //作用到速度
        verticalVelocity+=CurrentGravite*TickTime.deltaTime;
        if(verticalVelocity<actor.controllerSO.MaxfallSpeed)verticalVelocity=actor.controllerSO.MaxfallSpeed;
    }
}
