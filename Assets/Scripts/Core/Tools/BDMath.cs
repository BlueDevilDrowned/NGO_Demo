using UnityEngine;

public static class BDMath
{
    private const float DirectionSqrEpsilon=0.0001f;
    //输入校准到相机向前的位置
    public static Vector3 CalculateCameraRelativeMoveDirection(
        Vector2 input,
        float viewYaw)
    {
        if(input.sqrMagnitude<=DirectionSqrEpsilon)return Vector3.zero;

        Quaternion viewRotation=
            Quaternion.AngleAxis(viewYaw,Vector3.up);
        Vector3 worldDirection=
            viewRotation*new Vector3(input.x,0f,input.y);
        worldDirection.y=0f;

        return worldDirection.sqrMagnitude>DirectionSqrEpsilon
            ?worldDirection.normalized
            :Vector3.zero;
    }
    //基于上面的方法，计算一个向量到另一个向量所需角度180——-180
    public static float CalculateSignedPlanarAngle(
        Vector3 from,
        Vector3 to,
        Vector3 planeNormal)
    {
        if(planeNormal.sqrMagnitude<=DirectionSqrEpsilon)return 0f;

        planeNormal.Normalize();
        from=Vector3.ProjectOnPlane(from,planeNormal);
        to=Vector3.ProjectOnPlane(to,planeNormal);

        if(from.sqrMagnitude<=DirectionSqrEpsilon||
           to.sqrMagnitude<=DirectionSqrEpsilon)return 0f;

        return Vector3.SignedAngle(from,to,planeNormal);
    }
}
