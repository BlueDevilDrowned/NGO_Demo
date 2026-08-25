using System;

public interface IActorSystem : IDisposable//需要被释放
{
}

public interface IActorOwnershipSystem : IActorSystem//除此之外还需要完成所有权变化的问题
{
    void OnGainedOwnership();
    void OnLostOwnership();
}
