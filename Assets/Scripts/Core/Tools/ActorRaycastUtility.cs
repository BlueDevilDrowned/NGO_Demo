using UnityEngine;

/// <summary>
/// 射线检测工具类，提供忽略特定Actor的射线检测功能
/// </summary>
public static class ActorRaycastUtility
{
    public static bool TryRaycastIgnoringActor(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        int layerMask,
        QueryTriggerInteraction queryTriggerInteraction,
        Actor ignoredActor,
        RaycastHit[] hitBuffer,
        out RaycastHit closestHit)
    {
        closestHit=default;
        if(hitBuffer==null||hitBuffer.Length==0||
           maxDistance<=0f||direction.sqrMagnitude<=0.000001f)
            return false;

        int hitCount=Physics.RaycastNonAlloc(
            origin,
            direction.normalized,
            hitBuffer,
            maxDistance,
            layerMask,
            queryTriggerInteraction);
        float closestDistance=float.PositiveInfinity;

        for(int i=0;i<hitCount;i++)
        {
            RaycastHit candidate=hitBuffer[i];
            if(IsOwnedByActor(candidate.collider,ignoredActor))continue;
            if(candidate.distance>=closestDistance)continue;

            closestDistance=candidate.distance;
            closestHit=candidate;
        }

        return !float.IsPositiveInfinity(closestDistance);
    }

    public static bool IsOwnedByActor(Collider collider,Actor actor)
    {
        if(collider==null||actor==null)return false;

        Transform hitTransform=collider.transform;
        if(hitTransform==actor.transform||
           hitTransform.IsChildOf(actor.transform))return true;

        return collider.TryGetComponent(out Hitbox hitbox)&&
               hitbox.Manager!=null&&
               hitbox.Manager.Owner==actor;
    }
}
