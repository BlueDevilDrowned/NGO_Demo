using Unity.Netcode;
using UnityEngine;

public interface IActorSycnChannel
{
    public ushort ChannelId{get;}
    public SycnDirection direction{get;}
    public SyncDataKind DataKind{get;}
    public SyncSchedule Schedule{get;}
    public bool HasPendingData{get;}
    public bool TryWrite(uint Tick,FastBufferWriter writer);
    public bool TryApply(uint Tick,FastBufferReader reader,int payloadEnd);
}
