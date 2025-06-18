using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Database;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class
        BuildingWindowResourceManager : TypeResourceManager<BuildingType, BuildingDataItem, BuildingEntityPrefabData>
    {
        [SerializeField] [CanBeNull] private string buildingTypeSuffix;
        [SerializeField] [CanBeNull] private string buildingStateSuffix;
        [SerializeField] [CanBeNull] private string functionButtonConjuringTierTypeSuffix;
        // [SerializeField] [CanBeNull] private string functionButtonGeneratingTierTypeSuffix;

        // Interface
        public static BuildingWindowResourceManager Instance;
        public readonly Dictionary<BuildingType, Sprite> BuildingGeneralTypeSprites = new();
        public readonly Dictionary<BuildingState, Sprite> BuildingStateSprites = new();
        public readonly Dictionary<Tier, Sprite> FunctionConjuringButtonSprites = new();
        // public readonly Dictionary<Tier, Sprite> FunctionGeneratingButtonSprites = new();


        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public override void LoadResources()
        {
            base.LoadResources();
            ResourceGroup.Add(ResourceLoadingUtils.LoadTypeSuffix<BuildingType, Sprite>(buildingTypeSuffix,
                result => { ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, BuildingGeneralTypeSprites); }));
            ResourceGroup.Add(ResourceLoadingUtils.LoadTypeSuffix<Tier, Sprite>(functionButtonConjuringTierTypeSuffix,
                result => { ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FunctionConjuringButtonSprites); }));
            // ResourceGroup.Add(CR.LoadTypeSuffix<Tier, Sprite>(functionButtonGeneratingTierTypeSuffix,
            //     result => { CR.OnTypeSuffixLoadComplete(result, FunctionGeneratingButtonSprites); }));
            ResourceGroup.Add(ResourceLoadingUtils.LoadTypeSuffix<BuildingState, Sprite>(buildingStateSuffix,
                result => { ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, BuildingStateSprites); }));
        }

        public override void UnloadResources()
        {
            base.UnloadResources();
            BuildingGeneralTypeSprites.Clear();
            BuildingStateSprites.Clear();
            FunctionConjuringButtonSprites.Clear();
            // FunctionGeneratingButtonSprites.Clear();
        }
    }
}