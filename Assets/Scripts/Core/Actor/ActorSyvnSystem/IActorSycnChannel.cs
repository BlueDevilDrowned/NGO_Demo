using Unity.Netcode;
using UnityEngine;

public interface IActorSycnChannel
{
    public ushort ChannelId{get;}
    public bool TryWrite(FastBufferWriter writer);
    public bool TryApply(FastBufferReader reader,int payloadEnd);
}
