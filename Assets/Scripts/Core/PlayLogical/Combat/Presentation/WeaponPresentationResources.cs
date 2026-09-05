using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object=UnityEngine.Object;

/// <summary>
/// 持有一种武器使用的Tracer、弹痕和命中特效对象池。
/// </summary>
internal sealed class WeaponPresentationResources : IDisposable
{
    private readonly Actor actor;
    private readonly WeaponSO config;
    private readonly Transform root;
    private readonly ObjectPool<WeaponTracerEffect> tracerPool;
    private readonly ObjectPool<WeaponTracerEffect> firstPersonTracerPool;
    private readonly ObjectPool<WeaponBulletMarkEffect> bulletMarkPool;
    private readonly Dictionary<ParticleSystem,ObjectPool<WeaponImpactParticleEffect>>
        impactParticlePools=new();
    private readonly Dictionary<uint,WeaponTracerEffect> activeTracers=new();
    private readonly Dictionary<uint,WeaponTracerEffect>
        activeFirstPersonTracers=new();
    private bool isDisposed;

    public WeaponPresentationResources(
        Transform parent,
        WeaponSO config,
        Actor actor)
    {
        this.actor=actor;
        this.config=config??throw new ArgumentNullException(nameof(config));
        GameObject rootObject=new($"{config.name} Presentation");
        root=rootObject.transform;
        root.SetParent(parent,false);

        int defaultCapacity=Mathf.Max(1,config.PoolDefaultCapacity);
        int maxSize=Mathf.Max(defaultCapacity,config.PoolMaxSize);
        if(config.TracerPrefab!=null)
        {
            tracerPool=new ObjectPool<WeaponTracerEffect>(
                CreateTracer,
                effect=>effect.gameObject.SetActive(true),
                ReleaseTracer,
                effect=>Object.Destroy(effect.gameObject),
                true,
                defaultCapacity,
                maxSize);
            firstPersonTracerPool=new ObjectPool<WeaponTracerEffect>(
                CreateTracer,
                effect=>effect.gameObject.SetActive(true),
                ReleaseTracer,
                effect=>Object.Destroy(effect.gameObject),
                true,
                defaultCapacity,
                maxSize);
        }

        if(config.BulletMarkPrefab!=null)
        {
            bulletMarkPool=new ObjectPool<WeaponBulletMarkEffect>(
                CreateBulletMark,
                effect=>effect.gameObject.SetActive(true),
                ReleaseBulletMark,
                effect=>Object.Destroy(effect.gameObject),
                true,
                defaultCapacity,
                maxSize);
        }

        CreateImpactParticlePools(defaultCapacity,maxSize);
    }

    public void Apply(in ShotData shotEvent,bool useFirstPerson)
    {
        if(isDisposed)return;
        if(shotEvent.EventType==ShotEventType.Spawn)
        {
            SpawnTracer(in shotEvent,useFirstPerson);
            return;
        }

        ResolveTracer(in shotEvent);
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        activeTracers.Clear();
        activeFirstPersonTracers.Clear();
        root.gameObject.SetActive(false);
        tracerPool?.Clear();
        firstPersonTracerPool?.Clear();
        bulletMarkPool?.Clear();
        foreach(ObjectPool<WeaponImpactParticleEffect> pool in
                impactParticlePools.Values)
            pool.Clear();
        impactParticlePools.Clear();
        Object.Destroy(root.gameObject);
    }

    private void SpawnTracer(in ShotData shotEvent,bool useFirstPerson)
    {
        if(useFirstPerson&&firstPersonTracerPool!=null&&
           TryBuildFirstPersonShot(in shotEvent,out ShotData firstPersonShot))
        {
            if(activeFirstPersonTracers.ContainsKey(
                   firstPersonShot.ProjectileId))
                return;

            WeaponTracerEffect firstPersonTracer=firstPersonTracerPool.Get();
            activeFirstPersonTracers.Add(
                firstPersonShot.ProjectileId,
                firstPersonTracer);
            firstPersonTracer.Play(
                in firstPersonShot,
                HandleFirstPersonTracerCompleted);
            return;
        }

        if(tracerPool==null||activeTracers.ContainsKey(shotEvent.ProjectileId))
            return;

        WeaponTracerEffect tracer=tracerPool.Get();
        activeTracers.Add(shotEvent.ProjectileId,tracer);
        tracer.Play(in shotEvent,HandleTracerCompleted);
    }

    private void ResolveTracer(in ShotData shotEvent)
    {
        if(activeFirstPersonTracers.TryGetValue(
               shotEvent.ProjectileId,
               out WeaponTracerEffect firstPersonTracer))
        {
            firstPersonTracer.Resolve(in shotEvent);
            return;
        }

        if(activeTracers.TryGetValue(
            shotEvent.ProjectileId,
            out WeaponTracerEffect tracer))
        {
            tracer.Resolve(in shotEvent);
            return;
        }

        if(shotEvent.EventType==ShotEventType.Hit&&shotEvent.HasHit)
            PlayImpact(
                shotEvent.EndPoint,
                shotEvent.HitNormal,
                shotEvent.HitLayer);
    }

    private WeaponTracerEffect CreateTracer()
    {
        WeaponTracerEffect effect=Object.Instantiate(config.TracerPrefab,root);
        effect.gameObject.SetActive(false);
        return effect;
    }

    private bool TryBuildFirstPersonShot(
        in ShotData authoritativeShot,
        out ShotData visualShot)
    {
        visualShot=authoritativeShot;

        Transform muzzle=actor?.weaponEquipment?.FirstPersonMuzzle;
        Vector3 direction=actor?.cameraSystem?.data.ViewDirection??Vector3.zero;
        if(direction.sqrMagnitude<=0.000001f||!IsFinite(direction))
            direction=authoritativeShot.EndPoint-authoritativeShot.Origin;
        if(direction.sqrMagnitude<=0.000001f||!IsFinite(direction))
            return false;

        Vector3 origin=muzzle!=null
            ?muzzle.position
            :actor?.cameraSystem?.data.ViewOrigin??authoritativeShot.Origin;
        if(!IsFinite(origin))return false;

        direction.Normalize();
        visualShot.Origin=origin;
        // WeaponTracerEffect only needs this point to determine launch direction.
        visualShot.EndPoint=origin+direction;
        visualShot.TracerSpeed=config.TracerSpeed;
        visualShot.Gravity=config.ProjectileGravity;
        visualShot.Range=config.Range;
        return true;
    }

    private WeaponBulletMarkEffect CreateBulletMark()
    {
        WeaponBulletMarkEffect effect=
            Object.Instantiate(config.BulletMarkPrefab,root);
        effect.gameObject.SetActive(false);
        return effect;
    }

    private void HandleTracerCompleted(WeaponTracerEffect tracer)
    {
        uint projectileId=tracer.ProjectileId;
        if(activeTracers.TryGetValue(projectileId,out WeaponTracerEffect active)&&
           active==tracer)
            activeTracers.Remove(projectileId);

        if(tracer.HasHit)
            PlayImpact(tracer.EndPoint,tracer.HitNormal,tracer.HitLayer);
        tracerPool.Release(tracer);
    }

    private void HandleFirstPersonTracerCompleted(
        WeaponTracerEffect tracer)
    {
        uint projectileId=tracer.ProjectileId;
        if(activeFirstPersonTracers.TryGetValue(
               projectileId,
               out WeaponTracerEffect active)&&
           active==tracer)
            activeFirstPersonTracers.Remove(projectileId);

        if(tracer.HasHit)
            PlayImpact(tracer.EndPoint,tracer.HitNormal,tracer.HitLayer);
        firstPersonTracerPool.Release(tracer);
    }

    private void CreateImpactParticlePools(int defaultCapacity,int maxSize)
    {
        if(config.ImpactRules==null)return;

        for(int ruleIndex=0;ruleIndex<config.ImpactRules.Length;ruleIndex++)
        {
            WeaponImpactPresentationRule rule=config.ImpactRules[ruleIndex];
            if(rule?.ParticlePrefabs==null)continue;

            for(int prefabIndex=0;
                prefabIndex<rule.ParticlePrefabs.Length;
                prefabIndex++)
            {
                ParticleSystem prefab=rule.ParticlePrefabs[prefabIndex];
                if(prefab==null||impactParticlePools.ContainsKey(prefab))continue;

                impactParticlePools.Add(
                    prefab,
                    new ObjectPool<WeaponImpactParticleEffect>(
                        ()=>CreateImpactParticle(prefab),
                        effect=>effect.gameObject.SetActive(true),
                        ReleaseImpactParticle,
                        effect=>Object.Destroy(effect.gameObject),
                        true,
                        defaultCapacity,
                        maxSize));
            }
        }
    }

    private WeaponImpactParticleEffect CreateImpactParticle(
        ParticleSystem prefab)
    {
        ParticleSystem instance=Object.Instantiate(prefab,root);
        WeaponImpactParticleEffect effect=
            instance.GetComponent<WeaponImpactParticleEffect>();
        if(effect==null)
            effect=instance.gameObject.AddComponent<WeaponImpactParticleEffect>();
        effect.gameObject.SetActive(false);
        return effect;
    }

    private void PlayImpact(Vector3 hitPoint,Vector3 hitNormal,byte hitLayer)
    {
        WeaponImpactPresentationRule rule=config.GetImpactRule(hitLayer);
        if(rule==null)return;

        if(rule.EnableBulletMark)
            PlayBulletMark(hitPoint,hitNormal);
        PlayImpactParticles(rule,hitPoint,hitNormal);
    }

    private void PlayImpactParticles(
        WeaponImpactPresentationRule rule,
        Vector3 hitPoint,
        Vector3 hitNormal)
    {
        if(rule.ParticlePrefabs==null)return;

        for(int i=0;i<rule.ParticlePrefabs.Length;i++)
        {
            ParticleSystem prefab=rule.ParticlePrefabs[i];
            if(prefab==null||
               !impactParticlePools.TryGetValue(
                   prefab,
                   out ObjectPool<WeaponImpactParticleEffect> pool))continue;

            WeaponImpactParticleEffect effect=pool.Get();
            effect.Play(
                hitPoint,
                hitNormal,
                rule.ParticleNormalOffset,
                rule.ParticleRotationOffset,
                completed=>pool.Release(completed));
        }
    }

    private void PlayBulletMark(Vector3 hitPoint,Vector3 hitNormal)
    {
        if(bulletMarkPool==null)return;

        WeaponBulletMarkEffect mark=bulletMarkPool.Get();
        mark.Play(
            hitPoint,
            hitNormal,
            config.BulletMarkLifetime,
            ReleaseCompletedBulletMark);
    }

    private void ReleaseCompletedBulletMark(WeaponBulletMarkEffect effect)
    {
        bulletMarkPool.Release(effect);
    }

    private static void ReleaseTracer(WeaponTracerEffect effect)
    {
        effect.ResetEffect();
        effect.gameObject.SetActive(false);
    }

    private static void ReleaseBulletMark(WeaponBulletMarkEffect effect)
    {
        effect.ResetEffect();
        effect.gameObject.SetActive(false);
    }

    private static void ReleaseImpactParticle(WeaponImpactParticleEffect effect)
    {
        effect.ResetEffect();
        effect.gameObject.SetActive(false);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
