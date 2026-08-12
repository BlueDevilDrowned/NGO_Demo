using UnityEngine;

/// <summary>
/// 武器目录类，用于管理和获取武器定义数据
/// </summary>
public static class WeaponCatalog
{
    // 武器数据库的资源路径
    private const string ResourcePath = "WeaponDatabase";
    // 武器数据库的静态实例
    private static WeaponDatabaseSO database;

    public static bool TryGet(ushort weaponId, out WeaponSO definition)
    {
        definition = null;
        if (weaponId == 0)
            return false;

        WeaponDatabaseSO source = GetDatabase();
        return source != null && source.TryGet(weaponId, out definition);
    }

    public static WeaponSO Get(ushort weaponId)
    {
        if (TryGet(weaponId, out WeaponSO definition))
            return definition;

        Debug.LogError($"Weapon ID is not registered: {weaponId}");
        return null;
    }

    private static WeaponDatabaseSO GetDatabase()
    {
        if (database == null)
            database = Resources.Load<WeaponDatabaseSO>(ResourcePath);

        if (database == null)
            Debug.LogError($"Weapon database is missing at Resources/{ResourcePath}.asset.");

        return database;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        database = null;
    }
}
