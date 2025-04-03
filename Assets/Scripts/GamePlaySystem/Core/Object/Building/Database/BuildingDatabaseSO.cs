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

        // Config
        public GameObject prefab;
        public int id;
        public int subKey;
        [TextArea(3, 10)]
        public string description;
        
        public AssetReferenceSprite sprite2D;
        
        // Get 
        public BuildingType BuildingType => prefab.GetComponent<BuildingAttributesAuthoring>().buildingType;
        public Tier Tier => prefab.GetComponent<InteractableAttributesAuthoring>().tier;
        public List<CostResourceTypeAmountPair> Costs=> prefab.GetComponent<CostAttributesAuthoring>().costResourceTypeAmount;
        public string GameplayName => prefab.GetComponent<InteractableAttributesAuthoring>().gameplayName;
        
        public virtual int GetSubtype()
        {
            return subKey;
        }
    }

    // [Serializable]
    // public class FortificationData : BuildingData
    // {
    //     public FortificationType fortificationType;
    //     public override int GetSubtype()
    //     {
    //         return (int)fortificationType;
    //     }
    // }
    //
    // [Serializable]
    // public class WorkshopData : BuildingData
    // {
    //     public WorkshopType workshopType;
    //     public override int GetSubtype()
    //     {
    //         return (int)workshopType;
    //     }
    // }
    //
    // [Serializable]
    // public class ConjuringShrineData : BuildingData
    // {
    //     public ConjuringShrineType conjuringShrineType;
    //     public override int GetSubtype()
    //     {
    //         return (int)conjuringShrineType;
    //     }
    // }
    //
    // [Serializable]
    // public class DwellingData : BuildingData
    // {
    //     public DwellingType dwellingType;
    //     public override int GetSubtype()
    //     {
    //         return (int)dwellingType;
    //     }
    // }
    //
    // [Serializable]
    // public class OrnamentData : BuildingData
    // {
    //     public OrnamentType ornamentType;
    //     public override int GetSubtype()
    //     {
    //         return (int)ornamentType;
    //     }
    // }
    
}