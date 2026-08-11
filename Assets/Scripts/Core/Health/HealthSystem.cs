using System;
using UnityEngine;

public sealed class HealthSystem : IProjectileHitReceiver
{
    private readonly Actor actor;

    public float CurrentHealth{get;private set;}
    public float MaxHealth{get;private set;}
    public float NormalizedHealth=>MaxHealth>Mathf.Epsilon
        ?CurrentHealth/MaxHealth
        :0f;
    public bool IsDead=>CurrentHealth<=0f;

    public event Action<float,float> HealthChanged;
    public event Action<ProjectileHitResult> Damaged;
    public event Action Died;

    public HealthSystem(Actor actor,float maxHealth)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        MaxHealth=SanitizeMaxHealth(maxHealth);
        CurrentHealth=MaxHealth;
    }

    public void ReceiveProjectileHit(in ProjectileHitResult hit)
    {
        if(!actor.IsServer||hit.Target!=actor||IsDead)return;

        float damage=SanitizeAmount(hit.Damage);
        if(damage<=0f)return;

        float previousHealth=CurrentHealth;
        SetHealth(CurrentHealth-damage,MaxHealth);
        Damaged?.Invoke(hit);
        if(previousHealth>0f&&IsDead)
            Died?.Invoke();
    }

    public bool TryHeal(float amount)
    {
        if(!actor.IsServer||IsDead)return false;

        float healing=SanitizeAmount(amount);
        if(healing<=0f||CurrentHealth>=MaxHealth)return false;

        SetHealth(CurrentHealth+healing,MaxHealth);
        return true;
    }

    public bool TrySetMaxHealth(float maxHealth,bool restoreToFull=false)
    {
        if(!actor.IsServer)return false;

        float sanitizedMax=SanitizeMaxHealth(maxHealth);
        float targetHealth=restoreToFull?sanitizedMax:CurrentHealth;
        SetHealth(targetHealth,sanitizedMax);
        return true;
    }

    public bool TryRestoreFullHealth()
    {
        if(!actor.IsServer||CurrentHealth>=MaxHealth)return false;

        SetHealth(MaxHealth,MaxHealth);
        return true;
    }

    internal void ApplyAuthoritativeSnapshot(
        float currentHealth,
        float maxHealth)
    {
        bool wasDead=IsDead;
        SetHealth(currentHealth,maxHealth);
        if(!wasDead&&IsDead)
            Died?.Invoke();
    }

    private void SetHealth(float currentHealth,float maxHealth)
    {
        float sanitizedMax=SanitizeMaxHealth(maxHealth);
        float sanitizedCurrent=Mathf.Clamp(
            IsFinite(currentHealth)?currentHealth:0f,
            0f,
            sanitizedMax);
        if(Mathf.Approximately(CurrentHealth,sanitizedCurrent)&&
           Mathf.Approximately(MaxHealth,sanitizedMax))return;

        float previousHealth=CurrentHealth;
        MaxHealth=sanitizedMax;
        CurrentHealth=sanitizedCurrent;
        HealthChanged?.Invoke(previousHealth,CurrentHealth);
    }

    private static float SanitizeMaxHealth(float value)
    {
        return IsFinite(value)?Mathf.Max(1f,value):1f;
    }

    private static float SanitizeAmount(float value)
    {
        return IsFinite(value)?Mathf.Max(0f,value):0f;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
