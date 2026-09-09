using System;
using System.Collections.Generic;

/// <summary>物品配置内的多态能力；不得在共享配置上保存实例状态。</summary>
[Serializable]
public abstract class ItemModule
{
    public abstract void CollectInteractionOptions(ItemInteractionContext context, List<ItemInteractionOption> options);
}
