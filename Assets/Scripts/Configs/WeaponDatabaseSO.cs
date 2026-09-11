using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Scriptable Objects/Weapon Database")]
public sealed class WeaponDatabaseSO : ScriptableObject
{
    [SerializeField] private WeaponSO[] weapons;
    [SerializeField] private InventoryItemRegistry itemRegistry;

    private Dictionary<ushort, WeaponSO> definitionsById;

    public InventoryItemRegistry ItemRegistry=>itemRegistry;

    public bool TryGet(ushort weaponId, out WeaponSO definition)
    {
        EnsureLookup();
        return definitionsById.TryGetValue(weaponId, out definition);
    }

    public bool TryGetId(WeaponSO weapon,out ushort id)
    {
        EnsureLookup(); id=0;
        if(weapon==null) return false;
        foreach(var pair in definitionsById)
            if(pair.Value==weapon) { id=pair.Key; return true; }
        return false;
    }
    private void EnsureLookup()
    {
        if (definitionsById != null)
            return;

        definitionsById = new Dictionary<ushort, WeaponSO>();
        if (weapons == null)
            return;

        if (weapons.Length > ushort.MaxValue)
            Debug.LogError($"Weapon registry exceeds {ushort.MaxValue} entries: {name}", this);

        for (int index = 0; index < weapons.Length && index < ushort.MaxValue; index++)
        {
            WeaponSO definition = weapons[index];
            ushort id = (ushort)(index + 1);
            if (definition == null)
                continue;

            if (!definitionsById.TryAdd(id, definition))
                Debug.LogError($"Duplicate weapon ID in {name}: {id}", this);
        }
    }

    private void OnValidate()
    {
        definitionsById = null;
    }
}
