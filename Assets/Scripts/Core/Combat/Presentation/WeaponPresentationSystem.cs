using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class WeaponPresentationSystem
{
    private readonly WeaponSO config;
    private readonly Transform root;
    private readonly ObjectPool<WeaponTracerEffect> tracerPool;
    private readonly ObjectPool<WeaponBulletMarkEffect> bulletMarkPool;
    private readonly Dictionary<uint,WeaponTracerEffect> activeTracers=new();
    private bool isDisposed;

    public WeaponPresentationSystem(Transform owner,WeaponSO config)
    {
        this.config=config;
        GameObject rootObject=new($"{owner.name} Weapon Presentation");
        root=rootObject.transform;

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
    }

    public void Apply(in ShotData shotEvent)
    {
        if(isDisposed)return;
        if(shotEvent.EventType==ShotEventType.Spawn)
        {
            SpawnTracer(in shotEvent);
            return;
        }

        ResolveTracer(in shotEvent);
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        activeTracers.Clear();
        root.gameObject.SetActive(false);
        tracerPool?.Clear();
        bulletMarkPool?.Clear();
        Object.Destroy(root.gameObject);
    }

    private void SpawnTracer(in ShotData shotEvent)
    {
        if(tracerPool==null||activeTracers.ContainsKey(shotEvent.ProjectileId))
            return;

        WeaponTracerEffect tracer=tracerPool.Get();
        activeTracers.Add(shotEvent.ProjectileId,tracer);
        tracer.Play(in shotEvent,HandleTracerCompleted);
    }

    private void ResolveTracer(in ShotData shotEvent)
    {
        if(activeTracers.TryGetValue(
            shotEvent.ProjectileId,
            out WeaponTracerEffect tracer))
        {
            tracer.Resolve(in shotEvent);
            return;
        }

        if(shotEvent.EventType==ShotEventType.Hit&&shotEvent.HasHit)
            PlayBulletMark(shotEvent.EndPoint,shotEvent.HitNormal);
    }

    private WeaponTracerEffect CreateTracer()
    {
        WeaponTracerEffect effect=Object.Instantiate(config.TracerPrefab,root);
        effect.gameObject.SetActive(false);
        return effect;
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
            PlayBulletMark(tracer.EndPoint,tracer.HitNormal);
        tracerPool.Release(tracer);
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
}
