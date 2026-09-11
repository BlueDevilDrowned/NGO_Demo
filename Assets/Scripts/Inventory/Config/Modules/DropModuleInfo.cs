using System;
using UnityEngine;

[Serializable]
public sealed class DropModuleInfo : ItemModuleInfo
{
    public GameObject DropPrefab;
    public override Type TargetModuleType => typeof(DropModule);
    public override UnityEngine.Object LookupKey => DropPrefab;
}
