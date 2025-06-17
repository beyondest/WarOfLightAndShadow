
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GamePlaySystem.Database;
using SparFlame.Database.Database.DatabaseDefination;
using SparFlame.Database.Resources.Scripts.Database.DatabaseDefination;
using SparFlame.GamePlaySystem.Building;
using UnityEngine;
using UnityEditor;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace SparFlame.Database
{
    public static class DatabaseManager
    {
        private static BuildingDatabaseSo _buildingDatabaseSo;
        private static UnitDatabaseSo _unitDatabaseSo;
        private static ResourceDatabaseSo _resourceDatabaseSo;
        private static EnvDatabaseSo _envDatabaseSo;
        private static EnvTypeSpawnDatabaseSo _envTypeSpawnDatabaseSo;
        private static EnemyAIDatabaseSo _enemyAIDatabaseSo;
        private static VFXDatabaseSo _vfxDatabaseSo;
        private static BuffDatabaseSo _buffDatabaseSo;
        private static HintDatabaseSo _hintDatabaseSo;
        private static CityDatabaseSo _cityDatabaseSo;
        
        public static BuildingDatabaseSo BuildingDatabaseSo =>
            _buildingDatabaseSo ??= LoadAndMergeDatabase<BuildingDatabaseSo, BuildingDataItem>("items");

        public static UnitDatabaseSo UnitDatabaseSo =>
            _unitDatabaseSo ??= LoadAndMergeDatabase<UnitDatabaseSo, UnitDataItem>("items");

        public static ResourceDatabaseSo ResourceDatabaseSo =>
            _resourceDatabaseSo ??= LoadAndMergeDatabase<ResourceDatabaseSo, ResourceDataItem>("items");


        public static EnvDatabaseSo EnvDatabaseSo =>
            _envDatabaseSo ??= LoadAndMergeDatabase<EnvDatabaseSo, EnvDataItem>("items");

        public static EnvTypeSpawnDatabaseSo EnvTypeSpawnDatabaseSo =>
            _envTypeSpawnDatabaseSo ??= LoadAndMergeDatabase<EnvTypeSpawnDatabaseSo, EnvDatabaseItem>("items");
        

        public static EnemyAIDatabaseSo EnemyAIDatabaseSo =>
            _enemyAIDatabaseSo ??= LoadAndMergeDatabase<EnemyAIDatabaseSo, EnemyAIWaveDataItem>("items");

        public static VFXDatabaseSo VFXDatabaseSo =>
            _vfxDatabaseSo ??= LoadAndMergeDatabase<VFXDatabaseSo, VFXDataItem>("items");
        
        public static BuffDatabaseSo BuffDatabaseSo =>
        _buffDatabaseSo ??= LoadAndMergeDatabase<BuffDatabaseSo, BuffDataItem>("items");
        
        public static HintDatabaseSo HintDatabaseSo =>
        _hintDatabaseSo ??= LoadAndMergeDatabase<HintDatabaseSo, HintDataItem>("items");
        
        public static CityDatabaseSo CityDatabaseSo =>
        _cityDatabaseSo ??= LoadAndMergeDatabase<CityDatabaseSo, CityDataItem>("items");
        
        // This method only works for general databases
        public static GeneralDatabase<TData> GetDatabaseSo<TData>() where TData : GeneralDataItem
        {
            if (typeof(TData) == typeof(BuildingDataItem))
            {
                return BuildingDatabaseSo as GeneralDatabase<TData>;
            }
            else if (typeof(TData) == typeof(UnitDataItem))
                return UnitDatabaseSo as GeneralDatabase<TData>;
            else if (typeof(TData) == typeof(ResourceDataItem))
                return ResourceDatabaseSo as GeneralDatabase<TData>;

            throw new ArgumentException("This method only works for BuildingDataItem, UnitDataItem, and ResourceDataItem.");
        }

    
        


        private static TDatabase LoadAndMergeDatabase<TDatabase, TItem>(string itemFieldName)
            where TDatabase : ScriptableObject, new()
        {
            // 从 Resources/Database 加载所有数据库资源
            TDatabase[] databases = UnityEngine.Resources.LoadAll<TDatabase>("Database");

            if (databases == null || databases.Length == 0)
            {
                Debug.LogError($"No database assets found for type {typeof(TDatabase).Name} in Resources/Database!");
                return null;
            }

            // 有id的按照idStart排序，没id的，后来的排前面
            List<TDatabase> dbList = databases.OrderBy(GetIdStartValue).ToList();

            // 生成新的 database 实例
            TDatabase mergedDatabase = ScriptableObject.CreateInstance<TDatabase>();

            // 设置 idStart 为最小值
            int minIdStart = dbList.Min(GetIdStartValue);
            SetIdStartValue(mergedDatabase, minIdStart);

            // 合并 items
            var mergedList = new List<TItem>();

            foreach (var db in dbList)
            {
                var type = db.GetType();
                var field = type.GetField(itemFieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null && typeof(IEnumerable<TItem>).IsAssignableFrom(field.FieldType))
                {
                    var items = (IEnumerable<TItem>)field.GetValue(db);
                    if (items != null)
                    {
                        mergedList.AddRange(items);
                    }
                }
                else
                {
                    Debug.LogError($"Database {db.name} doesn't have a correct field named {itemFieldName}");
                }
            }

            // 把 mergedList 赋回去
            var mergedField = mergedDatabase.GetType().GetField(itemFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (mergedField != null)
            {
                mergedField.SetValue(mergedDatabase, mergedList);
            }
            else
            {
                Debug.LogError($"Merged database does not have a field named {itemFieldName}");
            }

            return mergedDatabase;
        }


        private static int GetIdStartValue(object database)
        {
            var type = database.GetType();
            var field = type.GetField("idStart");
            if (field != null && field.FieldType == typeof(int))
            {
                return (int)field.GetValue(database);
            }

            return int.MaxValue;
        }

        private static void SetIdStartValue(object database, int idStart)
        {
            var type = database.GetType();
            var field = type.GetField("idStart");
            if (field != null && field.FieldType == typeof(int))
            {
                field.SetValue(database, idStart);
            }
        }


#if UNITY_EDITOR
        [MenuItem("Tools/Database/Reload All Databases")]
        public static void ReloadAllDatabases()
        {
            _buildingDatabaseSo = null;
            _unitDatabaseSo = null;
            _resourceDatabaseSo = null;
            _envDatabaseSo = null;
            _enemyAIDatabaseSo = null;
            _envTypeSpawnDatabaseSo = null;
            _vfxDatabaseSo = null;
            _buffDatabaseSo = null;
            _hintDatabaseSo = null;
            _cityDatabaseSo = null;
            _ = BuildingDatabaseSo;
            _ = UnitDatabaseSo;
            _ = ResourceDatabaseSo;
            _ = EnvDatabaseSo;
            _ = EnemyAIDatabaseSo;
            _ = EnvTypeSpawnDatabaseSo;
            _ = VFXDatabaseSo;
            _ = BuffDatabaseSo;
            _ = HintDatabaseSo;
            _ = CityDatabaseSo;
            Debug.Log(" All Databases reloaded successfully!");
        }
#endif
        
    }
}