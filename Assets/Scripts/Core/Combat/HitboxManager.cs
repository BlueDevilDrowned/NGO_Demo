using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HitboxManager : MonoBehaviour
{
    private static readonly MovementControlRequest RagdollMovementControl=
        MovementControlRequest.DisableAll;
    private static readonly AnimationControlRequest RagdollAnimationControl=
        AnimationControlRequest.Disable;

    [SerializeField]private Actor owner;
    [SerializeField]private Transform hitboxRoot;
    [SerializeField]private List<Hitbox> hitboxes=new();
    [SerializeField]private bool configureJoints=true;
    [SerializeField]private bool startRagdoll=false;

    private readonly Dictionary<Collider,Hitbox> hitboxesByCollider=new();
    private readonly List<Rigidbody> ragdollBodies=new();
    private readonly List<CharacterJoint> ragdollJoints=new();
    private bool ragdollEnabled;

    public Actor Owner=>owner;
    public Transform HitboxRoot=>hitboxRoot;
    public IReadOnlyList<Hitbox> Hitboxes=>hitboxes;
    public IReadOnlyList<Rigidbody> RagdollBodies=>ragdollBodies;
    public IReadOnlyList<CharacterJoint> RagdollJoints=>ragdollJoints;

    private void Awake()
    {
        if(owner==null)
            owner=GetComponentInParent<Actor>();

        Rebuild();
        ConfigureJointsFromHierarchy();
        SetRagdoll(startRagdoll);
    }

    private void OnValidate()
    {
        if(owner==null)
            owner=GetComponentInParent<Actor>();

        Rebuild();
    }

    public void Initialize(Actor actor)
    {
        owner=actor;
        Rebuild();
        ConfigureJointsFromHierarchy();
        SetRagdoll(startRagdoll);
    }

    public void SetHitboxRoot(Transform root)
    {
        hitboxRoot=root;
        Rebuild();
    }

    [ContextMenu("Ragdoll/Rebuild Bindings")]
    public void Rebuild()
    {
        for(int i=0;i<hitboxes.Count;i++)
        {
            Hitbox hitbox=hitboxes[i];
            if(hitbox!=null&&hitbox.Manager==this)
                hitbox.Bind(null);
        }

        hitboxes.Clear();
        hitboxesByCollider.Clear();
        ragdollBodies.Clear();
        ragdollJoints.Clear();

        if(hitboxRoot==null)return;

        Hitbox[] children=hitboxRoot.GetComponentsInChildren<Hitbox>(true);
        for(int i=0;i<children.Length;i++)
            Register(children[i]);

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
        if(!configureJoints)return;
        if(ragdollBodies.Count==0)
            Rebuild();

        for(int i=0;i<ragdollJoints.Count;i++)
        {
            CharacterJoint joint=ragdollJoints[i];
            if(joint==null)continue;

            joint.connectedBody=FindParentRigidbody(joint.transform);
            joint.autoConfigureConnectedAnchor=true;
            joint.enableCollision=false;
        }
    }

    public bool TryResolve(Collider hitCollider,out Hitbox hitbox)
    {
        hitbox=null;
        if(hitCollider==null)return false;

        if(hitboxesByCollider.TryGetValue(hitCollider,out hitbox))
            return hitbox!=null;

        Hitbox candidate=hitCollider.GetComponent<Hitbox>();
        if(candidate==null||!IsManaged(candidate.transform))return false;

        Register(candidate);
        hitbox=candidate;
        return true;
    }

    public float GetDamageMultiplier(HitLocation location)
    {
        return owner!=null&&owner.actorConfig!=null
            ?owner.actorConfig.GetDamageMultiplier(location)
            :1f;
    }

    public void SetAllHitboxTriggers(bool isTrigger)
    {
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
        SetAllHitboxTriggers(true);
    }

    [ContextMenu("Hitboxes/Disable All Triggers")]
    private void DisableAllHitboxTriggers()
    {
        SetAllHitboxTriggers(false);
    }

    public void SetRagdoll(bool enabled)
    {
        ragdollEnabled=enabled;

        if(enabled)
        {
            SetMovementControl(true);
            SetAnimationControl(true);

            SetAllHitboxTriggers(false);
            SetRagdollBodiesSimulated(true);
            return;
        }

        SetRagdollBodiesSimulated(false);
        SetAllHitboxTriggers(true);
        SetAnimationControl(false);
        SetMovementControl(false);
    }

    public bool IsRagdollEnabled=>ragdollEnabled;

    [ContextMenu("Ragdoll/Enable")]
    private void EnableRagdoll()
    {
        SetRagdoll(true);
    }

    [ContextMenu("Ragdoll/Disable")]
    private void DisableRagdoll()
    {
        SetRagdoll(false);
    }

    internal void Register(Hitbox hitbox)
    {
        if(hitbox==null||!IsManaged(hitbox.transform))return;

        HitboxManager currentManager=hitbox.Manager;
        if(currentManager!=null&&currentManager!=this)
            currentManager.Unregister(hitbox);

        hitbox.Bind(this);
        if(!hitboxes.Contains(hitbox))
            hitboxes.Add(hitbox);

        Collider hitCollider=hitbox.Collider;
        if(hitCollider!=null)
            hitboxesByCollider[hitCollider]=hitbox;
    }

    internal void Unregister(Hitbox hitbox)
    {
        if(hitbox==null)return;

        hitboxes.Remove(hitbox);
        Collider hitCollider=hitbox.Collider;
        if(hitCollider!=null&&
           hitboxesByCollider.TryGetValue(hitCollider,out Hitbox registered)&&
           registered==hitbox)
            hitboxesByCollider.Remove(hitCollider);

        if(hitbox.Manager==this)
            hitbox.Bind(null);
    }

    private bool IsManaged(Transform target)
    {
        return hitboxRoot!=null&&target!=null&&
               (target==hitboxRoot||target.IsChildOf(hitboxRoot));
    }

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

        CharacterController characterController=owner!=null
            ?owner.characterController!=null
                ?owner.characterController
                :owner.GetComponent<CharacterController>()
            :GetComponentInParent<CharacterController>();
        if(characterController!=null)
            characterController.enabled=!disabled;
    }

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

        Animator animator=owner!=null
            ?owner.GetComponentInChildren<Animator>(true)
            :GetComponentInParent<Animator>();
        if(animator!=null)
            animator.enabled=!disabled;
    }

    private void SetRagdollBodiesSimulated(bool simulated)
    {
        for(int i=0;i<ragdollBodies.Count;i++)
        {
            Rigidbody body=ragdollBodies[i];
            if(body==null)continue;

            if(simulated)
            {
                body.isKinematic=false;
                body.useGravity=true;
                continue;
            }

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
