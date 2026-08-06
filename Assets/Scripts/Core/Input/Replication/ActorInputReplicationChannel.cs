using System;
using UnityEngine;

public sealed class ActorInputReplicationChannel
    : ActorReplicationChannel<ActorInputCommand>
{
    public const ushort Id=1;

    private readonly RunTimeData runTimeData;
    private bool hasAppliedInput;
    private uint lastAppliedTick;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>ActorReplicationDirection.OwnerToServer;

    public ActorInputReplicationChannel(RunTimeData runTimeData)
    {
        this.runTimeData=runTimeData??throw new ArgumentNullException(nameof(runTimeData));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out ActorInputCommand payload)
    {
        payload=default;
        // 输入只能由这个 NetworkObject 的 Owner 产生。
        if(!context.IsOwner)return false;

        payload=runTimeData.Input;
        payload.Tick=context.Tick;
        return true;
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in ActorInputCommand payload)
    {
        // 客户端只负责提交，只有服务器有权把输入写入权威运行时数据。
        if(!context.IsServer)return;
        // 丢弃重复包和乱序到达的旧输入。
        if(hasAppliedInput&&payload.Tick<=lastAppliedTick)return;
        // 网络数据不能直接信任，先拒绝会污染后续数学计算的 NaN/Infinity。
        if(!IsFinite(payload.InputMove)||!IsFinite(payload.InputLook))return;
        if(float.IsNaN(payload.ViewYaw)||float.IsInfinity(payload.ViewYaw))return;

        ActorInputCommand validatedPayload=payload;
        // 先复制再修正，保持 in payload 的只读语义。
        validatedPayload.InputMove=Vector2.ClampMagnitude(validatedPayload.InputMove,1f);
        validatedPayload.ViewYaw=Mathf.Repeat(validatedPayload.ViewYaw,360f);

        runTimeData.Input=validatedPayload;
        lastAppliedTick=validatedPayload.Tick;
        hasAppliedInput=true;
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y);
    }
}
