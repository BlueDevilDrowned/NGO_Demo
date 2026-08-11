using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HitboxManager : MonoBehaviour
{
    [SerializeField]private Actor owner;
    [SerializeField]private Transform hitboxRoot;
    [SerializeField]private List<Hitbox> hitboxes=new();

    private readonly Dictionary<Collider,Hitbox> hitboxesByCollider=new();

    public Actor Owner=>owner;
    public Transform HitboxRoot=>hitboxRoot;
    public IReadOnlyList<Hitbox> Hitboxes=>hitboxes;

    private void Awake()
    {
        if(owner==null)
            owner=GetComponentInParent<Actor>();

        Rebuild();
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
    }

    public void SetHitboxRoot(Transform root)
    {
        hitboxRoot=root;
        Rebuild();
    }

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

        if(hitboxRoot==null)return;

        Hitbox[] children=hitboxRoot.GetComponentsInChildren<Hitbox>(true);
        for(int i=0;i<children.Length;i++)
            Register(children[i]);
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
}
