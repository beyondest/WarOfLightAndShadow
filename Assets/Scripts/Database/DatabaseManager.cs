using System;
using GamePlaySystem.Database;
using SparFlame.GamePlaySystem.Building;
using UnityEngine;

namespace SparFlame.Database
{
    public static class DatabaseManager
    {
        private static BuildingDatabaseSo _buildingDatabaseSo;
        private static UnitDatabaseSo _unitDatabaseSo;
        private static ResourceDatabaseSo _resourceDatabaseSo;
        
        public static BuildingDatabaseSo BuildingDatabaseSo =>
            _buildingDatabaseSo ??=
                Resources.Load<BuildingDatabaseSo>("Database/BuildingDatabase");
        public static UnitDatabaseSo UnitDatabaseSo =>
            _unitDatabaseSo ??= Resources.Load<UnitDatabaseSo>("Database/UnitDatabase");
        public static ResourceDatabaseSo ResourceDatabaseSo =>
            _resourceDatabaseSo ??= Resources.Load<ResourceDatabaseSo>("Database/ResourceDatabase");

        public static GeneralDatabase<TData> GetDatabaseSo<TData>() where TData : GeneralDataItem
        {
            if (typeof(TData) == typeof(BuildingDataItem))
            {
                return BuildingDatabaseSo as GeneralDatabase<TData>;
            }
            else if(typeof(TData) == typeof(UnitDataItem))
                return UnitDatabaseSo as GeneralDatabase<TData>;
            else if(typeof(TData) == typeof(ResourceDataItem))
                return ResourceDatabaseSo as GeneralDatabase<TData>;
            throw new NotImplementedException();
        }
    }
}