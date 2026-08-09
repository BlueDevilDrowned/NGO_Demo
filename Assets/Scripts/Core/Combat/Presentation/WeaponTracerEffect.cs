using System;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public sealed class WeaponTracerEffect : MonoBehaviour
{
    private TrailRenderer trail;
    private Vector3 velocity;
    private Vector3 acceleration;
    private Vector3 endPoint;
    private float speed;
    private float range;
    private float travelledDistance;
    private Action<WeaponTracerEffect> completed;
    private bool isResolved;
    private bool isPlaying;

    public uint ProjectileId{get;private set;}
    public bool HasHit{get;private set;}
    public Vector3 HitNormal{get;private set;}
    public Vector3 EndPoint=>endPoint;

    public void Play(
        in ShotData shot,
        Action<WeaponTracerEffect> onCompleted)
    {
        EnsureTrail();
        Vector3 direction=shot.EndPoint-shot.Origin;
        if(direction.sqrMagnitude<=0.000001f)
            direction=transform.forward;

        ProjectileId=shot.ProjectileId;
        transform.SetPositionAndRotation(
            shot.Origin,
            Quaternion.LookRotation(direction.normalized));
        speed=Mathf.Max(0.01f,shot.TracerSpeed);
        velocity=direction.normalized*speed;
        Vector3 gravityDirection=Physics.gravity.sqrMagnitude>0.000001f
            ?Physics.gravity.normalized
            :Vector3.down;
        acceleration=gravityDirection*Mathf.Max(0f,shot.Gravity);
        range=Mathf.Max(0.01f,shot.Range);
        travelledDistance=0f;
        endPoint=shot.Origin;
        HasHit=false;
        HitNormal=Vector3.zero;
        completed=onCompleted;
        isResolved=false;
        trail.Clear();
        trail.emitting=true;
        isPlaying=true;
    }

    public void Resolve(in ShotData shot)
    {
        if(!isPlaying)return;

        endPoint=shot.EndPoint;
        HasHit=shot.EventType==ShotEventType.Hit&&shot.HasHit;
        HitNormal=shot.HitNormal;
        isResolved=true;
    }

    public void ResetEffect()
    {
        isPlaying=false;
        isResolved=false;
        completed=null;
        ProjectileId=0;
        if(trail==null)return;

        trail.emitting=false;
        trail.Clear();
    }

    private void Update()
    {
        if(!isPlaying)return;

        float deltaTime=Time.deltaTime;
        Vector3 previousPosition=transform.position;
        Vector3 nextPosition;
        if(isResolved)
        {
            nextPosition=Vector3.MoveTowards(
                previousPosition,
                endPoint,
                speed*deltaTime);
        }
        else
        {
            nextPosition=previousPosition+
                velocity*deltaTime+
                0.5f*acceleration*deltaTime*deltaTime;
            velocity+=acceleration*deltaTime;
        }

        Vector3 movement=nextPosition-previousPosition;
        if(movement.sqrMagnitude>0.000001f)
        {
            transform.SetPositionAndRotation(
                nextPosition,
                Quaternion.LookRotation(movement.normalized));
            travelledDistance+=movement.magnitude;
        }

        if(isResolved)
        {
            if((transform.position-endPoint).sqrMagnitude<=0.000001f)
                Complete();
            return;
        }

        if(travelledDistance>=range)
            Complete();
    }

    private void Complete()
    {
        isPlaying=false;
        trail.emitting=false;
        Action<WeaponTracerEffect> callback=completed;
        completed=null;
        callback?.Invoke(this);
    }

    private void EnsureTrail()
    {
        if(trail==null)
            trail=GetComponent<TrailRenderer>();
    }
}
