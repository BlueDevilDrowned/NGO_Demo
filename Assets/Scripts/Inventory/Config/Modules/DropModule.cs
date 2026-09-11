using System;
using System.Collections.Generic;

[Serializable]
public sealed class DropModule : ItemModule
{
    public override Type InfoType => typeof(DropModuleInfo);
    public override void CollectInteractionOptions(InventoryItemDefinition item, List<ItemInteractionOption> options) { }
}
