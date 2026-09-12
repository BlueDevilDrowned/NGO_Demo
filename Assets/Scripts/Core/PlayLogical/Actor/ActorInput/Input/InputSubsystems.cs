using System.Collections.Generic;
using UnityEngine;

public interface IInputSubsystem { bool Enabled { get; } void Enable(); void Disable(); }
public sealed class InputSubsystemManager
{
    private readonly List<IInputSubsystem> subsystems = new();
    private IInputSubsystem main;
    public void SetMain(IInputSubsystem subsystem) { main = subsystem; Register(subsystem); }
    public void ActivateMain() { if (main != null) Activate(main); }
    public void Register(IInputSubsystem subsystem) { if (subsystem != null && !subsystems.Contains(subsystem)) subsystems.Add(subsystem); }
    public void Activate(IInputSubsystem subsystem) { foreach (var item in subsystems) if (item == subsystem) item.Enable(); else item.Disable(); }
}
public sealed class GameplayInputSubsystem : IInputSubsystem
{
    public bool Enabled { get; private set; }
    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;
}
public sealed class InventoryInputSubsystem : IInputSubsystem
{
    public bool Enabled { get; private set; }
    public System.Action OnRotate, OnCancel;
    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;
    public void Rotate() { if (Enabled) OnRotate?.Invoke(); }
    public void Cancel() { if (Enabled) OnCancel?.Invoke(); }
}
