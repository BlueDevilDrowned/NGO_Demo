using Unity.Netcode;
using UnityEngine;

public interface IActorSycnChannel
{
    public ushort ChannelId{get;}
    public bool TryWrite(uint Tick,FastBufferWriter writer);
    public bool TryApply(uint Tick,FastBufferReader reader,int payloadEnd);
}
