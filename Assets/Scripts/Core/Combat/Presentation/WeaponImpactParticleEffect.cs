using System;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class WeaponImpactParticleEffect : MonoBehaviour
{
    private ParticleSystem rootParticle;
    private Action<WeaponImpactParticleEffect> completed;
    private bool isPlaying;

    public void Play(
        Vector3 hitPoint,
        Vector3 hitNormal,
        float normalOffset,
        Vector3 rotationOffset,
        Action<WeaponImpactParticleEffect> onCompleted)
    {
        EnsureParticle();
        Vector3 normal=hitNormal.sqrMagnitude>0.000001f
            ?hitNormal.normalized
            :Vector3.forward;
        transform.SetPositionAndRotation(
            hitPoint+normal*Mathf.Max(0f,normalOffset),
            Quaternion.LookRotation(normal)*Quaternion.Euler(rotationOffset));
        completed=onCompleted;
        rootParticle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        rootParticle.Play(true);
        isPlaying=true;
    }

    public void ResetEffect()
    {
        EnsureParticle();
        isPlaying=false;
        completed=null;
        rootParticle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        if(!isPlaying||rootParticle.IsAlive(true))return;

        isPlaying=false;
        Action<WeaponImpactParticleEffect> callback=completed;
        completed=null;
        callback?.Invoke(this);
    }

    private void EnsureParticle()
    {
        if(rootParticle==null)
            rootParticle=GetComponent<ParticleSystem>();
    }
}
