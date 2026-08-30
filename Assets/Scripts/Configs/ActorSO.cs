using UnityEngine;

[CreateAssetMenu(fileName = "ActorSO", menuName = "Scriptable Objects/ActorSO")]
public class ActorSO : ScriptableObject
{
    [Header("Core")]
    public ActorConfig actorConfig;
    public ActorBrainSo actorBrainSO;
    public ControllerSO controllerSO;

    [Header("View and Interaction")]
    public CameraSO cameraSO;
    public AimSO aimSO;
    public InteractSO interactSO;

    [Header("Presentation")]
    public FullBodyAnimationSO fullBodyAnimation;
    public AnimationSO animationSO;
    public ActorAudioMap audioMap;
    [Header("StartWeapon")]
    public int WeaponId=-1;
}
