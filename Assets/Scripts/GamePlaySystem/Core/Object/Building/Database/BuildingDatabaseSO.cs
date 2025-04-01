using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SparFlame.GamePlaySystem.Building
{
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "GameData/BuildingDatabase", order = 0)]
    public class BuildingDatabaseSo : ScriptableObject
    {
        
        public List<BuildingData> buildingsData;
        
        
    }
    
    
    [Serializable]
    public class BuildingData
    {
        
        [Header("General")]
        public string buildingName;
        public int id;
        [TextArea(3, 10)]
        public string description;
        
        [Header("Asset")]
        public GameObject prefab;
        public AssetReferenceSprite sprite2D;

        
        [Header("Gameplay")]
        public BuildingType buildingType;
        public Tier tier;
        public List<CostResourceTypeAmountPair> costs;
        
        public virtual int GetSubtype()
        {
            return 0;
        }
    }

    [Serializable]
    public class FortificationData : BuildingData
    {
        public FortificationType fortificationType;
        public override int GetSubtype()
        {
            return (int)fortificationType;
        }
    }

    [Serializable]
    public class WorkshopData : BuildingData
    {
        public WorkshopType workshopType;
        public override int GetSubtype()
        {
            return (int)workshopType;
        }
    }

    [Serializable]
    public class ConjuringShrineData : BuildingData
    {
        public ConjuringShrineType conjuringShrineType;
        public override int GetSubtype()
        {
            return (int)conjuringShrineType;
        }
    }

    [Serializable]
    public class DwellingData : BuildingData
    {
        public DwellingType dwellingType;
        public override int GetSubtype()
        {
            return (int)dwellingType;
        }
    }

    [Serializable]
    public class OrnamentData : BuildingData
    {
        public OrnamentType ornamentType;
        public override int GetSubtype()
        {
            return (int)ornamentType;
        }
    }
    
}