using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Scriptable Objects/Weapon Database")]
public sealed class WeaponDatabaseSO : ScriptableObject
{
    [SerializeField] private WeaponSO[] weapons;

    private Dictionary<ushort, WeaponSO> definitionsById;

    public bool TryGet(ushort weaponId, out WeaponSO definition)
    {
        EnsureLookup();
        return definitionsById.TryGetValue(weaponId, out definition);
    }

    private void EnsureLookup()
    {
        if (definitionsById != null)
            return;

        definitionsById = new Dictionary<ushort, WeaponSO>();
        if (weapons == null)
            return;

        foreach (WeaponSO definition in weapons)
        {
            if (definition == null || definition.Id == 0)
                continue;

            if (!definitionsById.TryAdd(definition.Id, definition))
                Debug.LogError($"Duplicate weapon ID in {name}: {definition.Id}", this);
        }
    }

    private void OnValidate()
    {
        definitionsById = null;
    }
}
