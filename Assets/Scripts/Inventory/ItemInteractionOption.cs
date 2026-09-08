using System;

public sealed class ItemInteractionOption
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool Available { get; }
    public Action Execute { get; }

    public ItemInteractionOption(string id, string displayName, bool available, Action execute)
    { Id = id; DisplayName = displayName; Available = available; Execute = execute; }
}
