using UnityEngine;

public sealed class ActorInputCommandConsumer
    : IReplicationConsumer<ActorInputCommand>
{
    private bool hasReceivedCommand;
    private uint lastReceivedTick;
    private bool hasPendingCommand;
    private ActorInputCommand pendingCommand;

    public void Receive(
        in ActorReplicationContext context,
        in ActorInputCommand command)
    {
        if(!context.IsServer)return;
        if(hasReceivedCommand&&command.Tick<=lastReceivedTick)return;
        if(!IsValid(in command.Data))return;

        ActorInputCommand validatedCommand=command;
        validatedCommand.Data.InputMove=
            Vector2.ClampMagnitude(validatedCommand.Data.InputMove,1f);
        validatedCommand.Data.ViewYaw=
            Mathf.Repeat(validatedCommand.Data.ViewYaw,360f);

        if(hasPendingCommand)
            validatedCommand.Data.Pressed|=pendingCommand.Data.Pressed;

        lastReceivedTick=validatedCommand.Tick;
        hasReceivedCommand=true;
        pendingCommand=validatedCommand;
        hasPendingCommand=true;
    }

    public bool TryConsume(out ActorInputCommand command)
    {
        command=default;
        if(!hasPendingCommand)return false;

        command=pendingCommand;
        hasPendingCommand=false;
        return true;
    }

    private static bool IsValid(in ActorInputData data)
    {
        return IsFinite(data.InputMove)&&
               IsFinite(data.InputLook)&&
               !float.IsNaN(data.ViewYaw)&&
               !float.IsInfinity(data.ViewYaw);
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y);
    }
}
