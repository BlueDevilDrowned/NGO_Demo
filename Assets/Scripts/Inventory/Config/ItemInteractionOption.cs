using UnityEngine;
public sealed class ItemInteractionOption
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool Available { get; }
    public ItemModule SourceModule { get; }
    public Sprite Icon { get; }
    public ItemInteractionOption(string id, string displayName, bool available, ItemModule sourceModule, Sprite icon = null)
    { Id=id; DisplayName=displayName; Available=available; SourceModule=sourceModule; Icon=icon; }
}
