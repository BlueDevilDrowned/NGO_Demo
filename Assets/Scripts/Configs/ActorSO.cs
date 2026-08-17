using UnityEngine;

[CreateAssetMenu(fileName = "ActorSO", menuName = "Scriptable Objects/ActorSO")]
public class ActorSO : ScriptableObject
{
    public CameraSO cameraSO;
    public AimSO aimSO;
}
