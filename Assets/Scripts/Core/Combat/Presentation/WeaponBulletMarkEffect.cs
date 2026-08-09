using System;
using UnityEngine;

/// <summary>
/// 封装了一个武器子弹痕迹效果的类，用于在命中点生成并显示子弹痕迹效果
/// </summary>
public sealed class WeaponBulletMarkEffect : MonoBehaviour
{
    // 完成回调的委托
    private Action<WeaponBulletMarkEffect> completed;
    // 剩余生命周期
    private float remainingLifetime;
    // 是否正在播放效果
    private bool isPlaying;

    public void Play(
        Vector3 hitPoint,
        Vector3 hitNormal,
        float lifetime,
        Action<WeaponBulletMarkEffect> onCompleted)
    {
        Vector3 normal=hitNormal.sqrMagnitude>0.000001f
            ?hitNormal.normalized
            :Vector3.forward;
        transform.SetPositionAndRotation(
            hitPoint+normal*0.002f,
            Quaternion.LookRotation(normal));
        remainingLifetime=Mathf.Max(0.01f,lifetime);
        completed=onCompleted;
        isPlaying=true;
    }

    public void ResetEffect()
    {
        isPlaying=false;
        completed=null;
        remainingLifetime=0f;
    }

    private void Update()
    {
        if(!isPlaying)return;

        remainingLifetime-=Time.deltaTime;
        if(remainingLifetime>0f)return;

        isPlaying=false;
        Action<WeaponBulletMarkEffect> callback=completed;
        completed=null;
        callback?.Invoke(this);
    }
}
