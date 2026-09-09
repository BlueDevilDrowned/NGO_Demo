using System;
using System.Collections.Generic;
using InventorySolver;

[Serializable]
public sealed class InventoryData
{
    [Serializable]
    public sealed class Entry
    {
        public int instanceId;
        public string itemId;
        public Placement placement;
    }

    public List<Entry> entries = new List<Entry>();
}
