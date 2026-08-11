using System.Collections.Generic;
using UnityEngine;

// 禁止在同一GameObject上添加多个此组件
[DisallowMultipleComponent]
// 使用sealed防止类被继承，优化性能
public sealed class HitboxManager : MonoBehaviour
{
    // 静态只读字段，定义布娃娃系统的运动和动画控制请求
    private static readonly MovementControlRequest RagdollMovementControl=
        MovementControlRequest.DisableAll;
    private static readonly AnimationControlRequest RagdollAnimationControl=
        AnimationControlRequest.Disable;



    // 序列化字段，可在编辑器中设置
    [SerializeField]private Actor owner;                    // 拥有此碰撞体管理器的角色
    [SerializeField]private Transform hitboxRoot;           // 碰撞体根节点
    [SerializeField]private List<Hitbox> hitboxes=new();   // 碰撞体列表
    [SerializeField]private bool configureJoints=true;     // 是否配置关节
    [SerializeField]private bool startRagdoll=false;       // 是否在启动时启用布娃娃系统



    // 只读字段，用于存储碰撞体和布娃娃部件的映射关系
    private readonly Dictionary<Collider,Hitbox> hitboxesByCollider=new();  // 通过碰撞器查找对应的碰撞体
    private readonly List<Rigidbody> ragdollBodies=new();                  // 布娃娃系统的刚体列表
    private readonly List<CharacterJoint> ragdollJoints=new();           // 布娃娃系统的关节列表
    private bool ragdollEnabled;                                         // 布娃娃系统是否启用



    // 属性，提供对私有字段的只读访问
    public Actor Owner=>owner;                                          // 获取拥有者
    public Transform HitboxRoot=>hitboxRoot;                            // 获取碰撞体根节点
    public IReadOnlyList<Hitbox> Hitboxes=>hitboxes;                    // 获取碰撞体列表
    public IReadOnlyList<Rigidbody> RagdollBodies=>ragdollBodies;       // 获取布娃娃刚体列表
    public IReadOnlyList<CharacterJoint> RagdollJoints=>ragdollJoints;  // 获取布娃娃关节列表

    // 生命周期方法
    private void Awake()
    {
        // 如果拥有者未设置，则尝试在父级组件中查找
        if(owner==null)
            owner=GetComponentInParent<Actor>();

        // 重建碰撞体系统
        Rebuild();
        // 从层级结构配置关节
        ConfigureJointsFromHierarchy();
        // 设置布娃娃系统状态
        SetRagdoll(startRagdoll);
    }

    private void OnValidate()
    {
        // 在编辑器模式下验证时，确保拥有者被正确设置
        if(owner==null)
            owner=GetComponentInParent<Actor>();

        // 重建碰撞体系统
        Rebuild();
    }

    // 公共方法
    public void Initialize(Actor actor)
    {
        // 初始化拥有者并重建系统
        owner=actor;
        Rebuild();
        ConfigureJointsFromHierarchy();
        SetRagdoll(startRagdoll);
    }

    public void SetHitboxRoot(Transform root)
    {
        // 设置碰撞体根节点并重建系统
        hitboxRoot=root;
        Rebuild();
    }

    // 通过菜单命令可以调用的方法
    [ContextMenu("Ragdoll/Rebuild Bindings")]
    public void Rebuild()
    {
        // 清理现有的绑定
        for(int i=0;i<hitboxes.Count;i++)
        {
            Hitbox hitbox=hitboxes[i];
            if(hitbox!=null&&hitbox.Manager==this)
                hitbox.Bind(null);
        }

        // 清空所有列表
        hitboxes.Clear();
        hitboxesByCollider.Clear();
        ragdollBodies.Clear();
        ragdollJoints.Clear();

        // 如果没有设置根节点，直接返回
        if(hitboxRoot==null)return;

        // 获取所有子碰撞体并注册
        Hitbox[] children=hitboxRoot.GetComponentsInChildren<Hitbox>(true);
        for(int i=0;i<children.Length;i++)
            Register(children[i]);

        // 收集所有刚体
        for(int i=0;i<hitboxes.Count;i++)
        {
            Collider hitCollider=hitboxes[i].Collider;
            if(hitCollider==null)continue;

            Rigidbody body=hitCollider.attachedRigidbody;
            if(body==null)
                body=hitCollider.GetComponentInParent<Rigidbody>();
            if(body!=null&&!ragdollBodies.Contains(body))
                ragdollBodies.Add(body);
        }

        // 收集所有关节
        for(int i=0;i<ragdollBodies.Count;i++)
        {
            Rigidbody body=ragdollBodies[i];
            CharacterJoint joint=body.GetComponent<CharacterJoint>();
            if(joint==null)continue;

            ragdollJoints.Add(joint);
        }
    }

    [ContextMenu("Ragdoll/Configure Joints From Hierarchy")]
    public void ConfigureJointsFromHierarchy()
    {
        // 如果不配置关节或没有刚体，则返回
        if(!configureJoints)return;
        if(ragdollBodies.Count==0)
            Rebuild();

        // 配置每个关节的参数
        for(int i=0;i<ragdollJoints.Count;i++)
        {
            CharacterJoint joint=ragdollJoints[i];
            if(joint==null)continue;

            // 设置连接的刚体，自动配置锚点，禁用碰撞
            joint.connectedBody=FindParentRigidbody(joint.transform);
            joint.autoConfigureConnectedAnchor=true;
            joint.enableCollision=false;
        }
    }

    public bool TryResolve(Collider hitCollider,out Hitbox hitbox)
    {
        hitbox=null;
        if(hitCollider==null)return false;

        // 尝试从字典中查找碰撞体
        if(hitboxesByCollider.TryGetValue(hitCollider,out hitbox))
            return hitbox!=null;

        // 如果找不到，尝试获取组件并检查是否受管理
        Hitbox candidate=hitCollider.GetComponent<Hitbox>();
        if(candidate==null||!IsManaged(candidate.transform))return false;

        // 注册候选碰撞体并返回
        Register(candidate);
        hitbox=candidate;
        return true;
    }

    public float GetDamageMultiplier(HitLocation location)
    {
        // 获取指定位置的伤害倍率
        return owner!=null&&owner.actorConfig!=null
            ?owner.actorConfig.GetDamageMultiplier(location)
            :1f;
    }

    public void SetAllHitboxTriggers(bool isTrigger)
    {
        // 设置所有碰撞体的触发器状态
        for(int i=0;i<hitboxes.Count;i++)
        {
            Collider hitCollider=hitboxes[i]?.Collider;
            if(hitCollider!=null)
                hitCollider.isTrigger=isTrigger;
        }
    }

    [ContextMenu("Hitboxes/Enable All Triggers")]
    private void EnableAllHitboxTriggers()
    {
        // 启用所有触发器
        SetAllHitboxTriggers(true);
    }

    [ContextMenu("Hitboxes/Disable All Triggers")]
    private void DisableAllHitboxTriggers()
    {
        // 禁用所有触发器
        SetAllHitboxTriggers(false);
    }

    public void SetRagdoll(bool enabled)
    {
        // 设置布娃娃系统的启用状态
        ragdollEnabled=enabled;

        if(enabled)
        {
            // 启用布娃娃系统时的设置
            SetMovementControl(true);
            SetAnimationControl(true);

            SetAllHitboxTriggers(false);
            SetRagdollBodiesSimulated(true);
            return;
        }

        // 禁用布娃娃系统时的设置
        SetRagdollBodiesSimulated(false);
        SetAllHitboxTriggers(true);
        SetAnimationControl(false);
        SetMovementControl(false);
    }

    public bool IsRagdollEnabled=>ragdollEnabled;

    [ContextMenu("Ragdoll/Enable")]
    private void EnableRagdoll()
    {
        // 启用布娃娃系统
        SetRagdoll(true);
    }

    [ContextMenu("Ragdoll/Disable")]
    private void DisableRagdoll()
    {
        // 禁用布娃娃系统
        SetRagdoll(false);
    }

    // 内部方法，用于注册和注销碰撞体
    internal void Register(Hitbox hitbox)
    {
        if(hitbox==null||!IsManaged(hitbox.transform))return;

        // 如果碰撞体已经有其他管理器，先注销
        HitboxManager currentManager=hitbox.Manager;
        if(currentManager!=null&&currentManager!=this)
            currentManager.Unregister(hitbox);

        // 绑定到此管理器并添加到列表
        hitbox.Bind(this);
        if(!hitboxes.Contains(hitbox))
            hitboxes.Add(hitbox);

        // 更新碰撞器到碰撞体的映射
        Collider hitCollider=hitbox.Collider;
        if(hitCollider!=null)
            hitboxesByCollider[hitCollider]=hitbox;
    }

    internal void Unregister(Hitbox hitbox)
    {
        if(hitbox==null)return;

        // 从列表中移除碰撞体
        hitboxes.Remove(hitbox);
        // 从映射中移除碰撞器
        Collider hitCollider=hitbox.Collider;
        if(hitCollider!=null&&
           hitboxesByCollider.TryGetValue(hitCollider,out Hitbox registered)&&
           registered==hitbox)
            hitboxesByCollider.Remove(hitCollider);

        // 如果碰撞体仍绑定到此管理器，解除绑定
        if(hitbox.Manager==this)
            hitbox.Bind(null);
    }

    // 检查目标变换是否受此管理器管理
    private bool IsManaged(Transform target)
    {
        return hitboxRoot!=null&&target!=null&&
               (target==hitboxRoot||target.IsChildOf(hitboxRoot));
    }

    // 查找父级刚体
    private static Rigidbody FindParentRigidbody(Transform child)
    {
        Transform current=child.parent;
        while(current!=null)
        {
            Rigidbody body=current.GetComponent<Rigidbody>();
            if(body!=null)return body;
            current=current.parent;
        }

        return null;
    }

    // 设置运动控制
    private void SetMovementControl(bool disabled)
    {
        if(owner!=null&&owner.movement!=null)
        {
            if(disabled)
                owner.movement.SubmitControlRequest(this,in RagdollMovementControl);
            else
                owner.movement.RemoveControlRequest(this);
            return;
        }

        // 如果没有运动组件，尝试使用角色控制器
        CharacterController characterController=owner!=null
            ?owner.characterController!=null
                ?owner.characterController
                :owner.GetComponent<CharacterController>()
            :GetComponentInParent<CharacterController>();
        if(characterController!=null)
            characterController.enabled=!disabled;
    }

    // 设置动画控制
    private void SetAnimationControl(bool disabled)
    {
        if(owner!=null&&owner.animationArbiter!=null)
        {
            if(disabled)
                owner.animationArbiter.SubmitControlRequest(
                    this,
                    in RagdollAnimationControl);
            else
                owner.animationArbiter.RemoveControlRequest(this);
            return;
        }

        // 如果没有动画仲裁器，尝试使用动画器
        Animator animator=owner!=null
            ?owner.GetComponentInChildren<Animator>(true)
            :GetComponentInParent<Animator>();
        if(animator!=null)
            animator.enabled=!disabled;
    }

    // 设置布娃娃刚体的物理模拟状态
    private void SetRagdollBodiesSimulated(bool simulated)
    {
        for(int i=0;i<ragdollBodies.Count;i++)
        {
            Rigidbody body=ragdollBodies[i];
            if(body==null)continue;

            if(simulated)
            {
                // 启用物理模拟
                body.isKinematic=false;
                body.useGravity=true;
                continue;
            }

            // 禁用物理模拟时重置速度
            if(!body.isKinematic)
            {
                body.linearVelocity=Vector3.zero;
                body.angularVelocity=Vector3.zero;
            }

            body.useGravity=false;
            body.isKinematic=true;
        }
    }
}
