using Animancer;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimancerData", menuName = "Scriptable Objects/AnimancerData")]
public class AnimancerData : ScriptableObject
{
    public TransitionAsset Idle;
    public TransitionAsset Walk_Start;
    public TransitionAsset Walk_Stop_L;
    public TransitionAsset Walk_Stop_R;
    public TransitionAsset Walk_Loop_L;
    public TransitionAsset Walk_Loop_R;
    public TransitionAsset Walk_Loop_Lean;
    public TransitionAsset Walk_Turn_LL;
    public TransitionAsset Walk_Turn_LR;
    public TransitionAsset Walk_Turn_RL;
    public TransitionAsset Walk_Turn_RR;
}
