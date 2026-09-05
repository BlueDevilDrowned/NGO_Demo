using System;
using UnityEngine;

public sealed class WeaponInventorySystem : IActorSystem
{
    private const float ScrollThreshold=0.01f;

    private readonly Actor actor;
    private readonly WeaponInventorySO config;
    private readonly WeaponInventoryReplication replication;
    private uint lastProcessedInputTick;
    private bool isDisposed;

    public WeaponInventoryData Data=>actor.simulation.weaponInventoryData;
    public WeaponInventorySO Config=>config;
    public byte CurrentIndex=>Data!=null?Data.currentIndex:(byte)0;
    public ushort CurrentWeaponId=>Data!=null
        ?Data.GetWeaponId(CurrentIndex)
        :(ushort)0;

    public event Action<byte> ActiveSlotChanged;
    public event Action InventoryChanged;
    public event Action Changed;

    public WeaponInventorySystem(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        config=actor.actorSO?.weaponInventorySO;
        actor.RegisterSystem(this);
        Initialize();
        replication=new(actor);
        if(actor.IsServer)
        {
            WeaponInventoryData data=Data;
            replication.MarkAuthoritativeState(in data,0);
        }
    }

    public void PresentationUpdate()
    {
        if(isDisposed||actor.IsServer||
           !replication.TryConsumeState(out _))return;

        InventoryChanged?.Invoke();
        ActiveSlotChanged?.Invoke(CurrentIndex);
        Changed?.Invoke();
    }

    public void ServerTick()
    {
        
        if(isDisposed||!actor.IsServer||Data==null)return;

        uint inputTick=actor.inputSystem.replication.LastReceivedInputTick;
        if(inputTick==0||inputTick==lastProcessedInputTick)return;
        lastProcessedInputTick=inputTick;

        if(actor.simulation.inputData.WasPressed(InputButtons.InputDrop))
            TryDropCurrentWeapon();

        float scroll=actor.simulation.inputData.InputScroll.y;
        if(Mathf.Abs(scroll)<ScrollThreshold)return;

        int direction=scroll>0f?1:-1;
        if(TryFindNextOccupiedSlot(direction,out byte nextSlot))
            TrySelectSlot(nextSlot);
    }

    public bool TrySelectSlot(byte slot)
    {
        if(isDisposed||!actor.IsServer||!IsOccupied(slot))return false;
        if(Data.currentIndex==slot)return true;

        Data.currentIndex=slot;
        MarkAuthoritativeState();
        ActiveSlotChanged?.Invoke(slot);
        Changed?.Invoke();
        return true;
    }

    public bool TryStoreWeapon(byte slot,ushort weaponId)
    {
        if(isDisposed||!actor.IsServer||weaponId==0||!IsValidSlot(slot))
            return false;
        if(!WeaponCatalog.TryGet(weaponId,out _))return false;

        ushort currentId=Data.GetWeaponId(slot);
        // A non-droppable item may be initialized, but can never be replaced.
        if(currentId!=0&&!CanDrop(slot))return false;

        Data.weaponIds[slot]=weaponId;
        if(!IsOccupied(Data.currentIndex))
            Data.currentIndex=slot;

        MarkAuthoritativeState();
        InventoryChanged?.Invoke();
        ActiveSlotChanged?.Invoke(Data.currentIndex);
        Changed?.Invoke();
        return true;
    }

    public bool TryPickupWeapon(ushort weaponId,out byte slot)
    {
        return TryPickupWeapon(weaponId,out slot,out _);
    }

    public bool TryPickupWeapon(
        ushort weaponId,
        out byte slot,
        out ushort replacedWeaponId)
    {
        slot=0;
        replacedWeaponId=0;
        if(isDisposed||!actor.IsServer||weaponId==0||config?.Slots==null)
            return false;

        for(int i=0;i<config.Slots.Count;i++)
        {
            WeaponSlotConfig slotConfig=config.Slots[i];
            if(slotConfig==null||
               slotConfig.Type!=WeaponSlotType.Primary||
               Data.GetWeaponId(i)!=0)
                continue;

            if(i>byte.MaxValue||!TryStoreWeapon((byte)i,weaponId))continue;

            slot=(byte)i;
            return true;
        }

        byte currentSlot=CurrentIndex;
        if(!CanReplace(currentSlot))return false;
        if(!TryReplaceWeapon(
               currentSlot,
               weaponId,
               out replacedWeaponId))
            return false;

        slot=currentSlot;
        return true;
    }

    public bool TryReplaceWeapon(
        byte slot,
        ushort weaponId,
        out ushort replacedWeaponId)
    {
        replacedWeaponId=0;
        if(isDisposed||!actor.IsServer||!IsValidSlot(slot))
            return false;

        replacedWeaponId=Data.GetWeaponId(slot);
        if(replacedWeaponId==0||!CanDrop(slot))
        {
            replacedWeaponId=0;
            return false;
        }

        if(!TryStoreWeapon(slot,weaponId))
        {
            replacedWeaponId=0;
            return false;
        }

        return true;
    }

    public bool TryDropWeapon(byte slot,out ushort weaponId)
    {
        weaponId=0;
        if(isDisposed||!actor.IsServer||!IsValidSlot(slot)||!CanDrop(slot))
            return false;

        weaponId=Data.GetWeaponId(slot);
        if(weaponId==0)return false;

        Data.weaponIds[slot]=0;
        if(Data.currentIndex==slot)
        {
            if(!TryFindFirstOccupiedSlot(out byte fallback))
                fallback=0;
            Data.currentIndex=fallback;
            MarkAuthoritativeState();
            ActiveSlotChanged?.Invoke(fallback);
        }
        else
            MarkAuthoritativeState();

        InventoryChanged?.Invoke();
        Changed?.Invoke();
        return true;
    }

    public bool CanDrop(byte slot)
    {
        return IsValidSlot(slot)&&config.Slots[slot].CanDrop;
    }

    public bool CanReplace(byte slot)
    {
        return IsValidSlot(slot)&&
               (Data.GetWeaponId(slot)==0||CanDrop(slot));
    }

    private bool TryDropCurrentWeapon()
    {
        byte slot=CurrentIndex;
        if(!TryDropWeapon(slot,out ushort weaponId))return false;

        SpawnDroppedWeapon(weaponId);
        return true;
    }

    private void SpawnDroppedWeapon(ushort weaponId)
    {
        Vector3 inheritedVelocity=actor.movement?.Velocity??Vector3.zero;
        float throwSpeed=actor.actorSO?.controllerSO?.WeaponDropThrowSpeed??0f;
        Vector3 dropVelocity=inheritedVelocity+
            actor.transform.forward*throwSpeed;
        ControllerSO controller=actor.actorSO?.controllerSO;
        Vector3 dropPosition=controller!=null
            ?controller.GetWeaponDropPosition(actor.transform)
            :actor.transform.position;

        WorldWeaponPickup.Spawn(
            weaponId,
            dropPosition,
            actor.transform.rotation,
            dropVelocity);
    }

    public ushort GetWeaponId(byte slot)
    {
        return Data?.GetWeaponId(slot)??0;
    }

    public bool TryFindNextOccupiedSlot(
        int direction,
        out byte slot)
    {
        slot=0;
        int count=SlotCount;
        if(count<=1)return false;

        int step=direction>=0?1:-1;
        int current=Mathf.Clamp(CurrentIndex,0,count-1);
        for(int offset=1;offset<count;offset++)
        {
            int candidate=(current+step*offset)%count;
            if(candidate<0)candidate+=count;
            if(IsOccupied((byte)candidate))
            {
                slot=(byte)candidate;
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        replication.Dispose();
        ActiveSlotChanged=null;
        InventoryChanged=null;
        Changed=null;
    }

    private int SlotCount=>Data?.weaponIds?.Count??0;

    private void MarkAuthoritativeState()
    {
        if(!actor.IsServer)return;

        WeaponInventoryData data=Data;
        replication.MarkAuthoritativeState(
            in data,
            actor.inputSystem.replication.LastReceivedInputTick);
    }

    private void Initialize()
    {
        WeaponInventoryData data=new();
        if(config?.Slots!=null)
        {
            for(int i=0;i<config.Slots.Count;i++)
            {
                WeaponSlotConfig slot=config.Slots[i];
                ushort weaponId=0;
                if(slot!=null&&slot.InitialWeaponId>0&&
                   slot.InitialWeaponId<=ushort.MaxValue&&
                   WeaponCatalog.TryGet(
                       (ushort)slot.InitialWeaponId,
                       out _))
                    weaponId=(ushort)slot.InitialWeaponId;

                data.weaponIds.Add(weaponId);
            }
        }

        if(!TryFindFirstOccupiedSlot(data,out byte firstOccupied))
            firstOccupied=0;
        data.currentIndex=firstOccupied;
        actor.simulation.weaponInventoryData=data;

        if(config==null)
            Debug.LogWarning(
                $"{actor.name} has no WeaponInventorySO; using a legacy single-slot inventory.",
                actor);
    }

    private bool IsValidSlot(byte slot)
    {
        return config?.Slots!=null&&
               slot<config.Slots.Count&&
               slot<Data.weaponIds.Count&&
               config.Slots[slot]!=null;
    }

    private bool IsOccupied(byte slot)
    {
        return IsValidSlot(slot)&&Data.GetWeaponId(slot)>0;
    }

    private bool TryFindFirstOccupiedSlot(out byte slot)
    {
        return TryFindFirstOccupiedSlot(Data,out slot);
    }

    private static bool TryFindFirstOccupiedSlot(
        WeaponInventoryData data,
        out byte slot)
    {
        slot=0;
        if(data?.weaponIds==null)return false;

        for(int i=0;i<data.weaponIds.Count;i++)
        {
            if(data.weaponIds[i]==0)continue;
            slot=(byte)i;
            return true;
        }

        return false;
    }
}
