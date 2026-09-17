using System;
using UnityEngine;

public enum WeaponTracerCompletionMode:byte
{
    LocalCollisionOrRange,
    AwaitAuthoritativeResult,
}

[RequireComponent(typeof(TrailRenderer))]
public sealed class WeaponTracerEffect : MonoBehaviour
{
    private readonly RaycastHit[] localHitBuffer=new RaycastHit[16];
    private TrailRenderer trail;
    private Vector3 velocity;
    private Vector3 acceleration;
    private Vector3 endPoint;
    private float speed;
    private float range;
    private float travelledDistance;
    private Action<WeaponTracerEffect> completed;
    private WeaponTracerCompletionMode completionMode;
    private int localCollisionMask;
    private Actor ignoredActor;
    private float unresolvedLifetime;
    private bool isResolved;
    private bool isPlaying;

    public uint ProjectileId{get;private set;}
    public uint ClientShotId{get;private set;}
    public bool HasHit{get;private set;}
    public byte HitLayer{get;private set;}
    public Vector3 HitNormal{get;private set;}
    public Vector3 EndPoint=>endPoint;

    public void Play(
        in ShotData shot,
        WeaponTracerCompletionMode mode,
        int collisionMask,
        Actor localIgnoredActor,
        Action<WeaponTracerEffect> onCompleted)
    {
        EnsureTrail();
        Vector3 direction=shot.EndPoint-shot.Origin;
        if(direction.sqrMagnitude<=0.000001f)
            direction=transform.forward;

        ProjectileId=shot.ProjectileId;
        ClientShotId=shot.ClientShotId;
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
        HitLayer=byte.MaxValue;
        HitNormal=Vector3.zero;
        completed=onCompleted;
        completionMode=mode;
        localCollisionMask=collisionMask;
        ignoredActor=localIgnoredActor;
        unresolvedLifetime=range/speed+5f;
        isResolved=false;
        trail.Clear();
        trail.emitting=true;
        isPlaying=true;
    }

    public void BindProjectile(uint projectileId)
    {
        if(!isPlaying||projectileId==0)return;

        ProjectileId=projectileId;
    }

    public void Resolve(in ShotData shot)
    {
        if(!isPlaying)return;

        endPoint=shot.EndPoint;
        HasHit=shot.EventType==ShotEventType.Hit&&shot.HasHit;
        HitLayer=shot.HitLayer;
        HitNormal=shot.HitNormal;
        isResolved=true;
    }

    public void ResetEffect()
    {
        isPlaying=false;
        isResolved=false;
        completed=null;
        ProjectileId=0;
        ClientShotId=0;
        localCollisionMask=0;
        ignoredActor=null;
        unresolvedLifetime=0f;
        HitLayer=byte.MaxValue;
        if(trail==null)return;

        trail.emitting=false;
        trail.Clear();
    }

    private void Update()
    {
        if(!isPlaying)return;

        float deltaTime=Time.deltaTime;
        if(!isResolved)
        {
            unresolvedLifetime-=deltaTime;
            if(unresolvedLifetime<=0f)
            {
                Complete();
                return;
            }
        }

        Vector3 previousPosition=transform.position;
        Vector3 nextPosition;
        bool reachedRange=false;
        bool localCollision=false;
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
            Vector3 unresolvedMovement=nextPosition-previousPosition;
            float unresolvedDistance=unresolvedMovement.magnitude;
            float remainingDistance=Mathf.Max(0f,range-travelledDistance);
            if(remainingDistance<=0.000001f)
            {
                nextPosition=previousPosition;
                reachedRange=true;
            }
            else if(unresolvedDistance>=remainingDistance&&
                    unresolvedDistance>0.000001f)
            {
                nextPosition=previousPosition+
                    unresolvedMovement/unresolvedDistance*remainingDistance;
                reachedRange=true;
            }
            velocity+=acceleration*deltaTime;
        }

        Vector3 movement=nextPosition-previousPosition;
        if(!isResolved&&
           completionMode==WeaponTracerCompletionMode.LocalCollisionOrRange&&
           movement.sqrMagnitude>0.000001f&&
           ActorRaycastUtility.TryRaycastIgnoringActor(
               previousPosition,
               movement.normalized,
               movement.magnitude,
               localCollisionMask,
               QueryTriggerInteraction.Collide,
               ignoredActor,
               localHitBuffer,
               out RaycastHit localHit))
        {
            localCollision=true;
            nextPosition=localHit.point;
            movement=nextPosition-previousPosition;
        }

        if(movement.sqrMagnitude>0.000001f)
        {
            transform.SetPositionAndRotation(
                nextPosition,
                Quaternion.LookRotation(movement.normalized));
            travelledDistance+=movement.magnitude;
        }

        if(!isResolved&&
           completionMode==WeaponTracerCompletionMode.LocalCollisionOrRange&&
           (localCollision||reachedRange))
        {
            Complete();
            return;
        }

        if(!isResolved&&reachedRange)
        {
            velocity=Vector3.zero;
            acceleration=Vector3.zero;
        }

        if(isResolved)
        {
            if((transform.position-endPoint).sqrMagnitude<=0.000001f)
                Complete();
            return;
        }

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
