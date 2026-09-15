using UnityEngine;

public sealed class BackpackInteractionOption
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool Available { get; }
    public BackpackInteractionModule SourceModule { get; }
    public Sprite Icon { get; }
    public BackpackInteractionOption(
        string id,
        string displayName,
        bool available,
        BackpackInteractionModule sourceModule,
        Sprite icon = null)
    {
        Id = id;
        DisplayName = displayName;
        Available = available;
        SourceModule = sourceModule;
        Icon = icon;
    }
}
