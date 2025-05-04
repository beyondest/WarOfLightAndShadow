using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.GamePlay
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
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public override void LoadResources()
        {
            base.LoadResources();
            ResourceGroup.Add(CR.LoadTypeSuffix<BuildingType, Sprite>(buildingTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, BuildingGeneralTypeSprites); }));
            ResourceGroup.Add(CR.LoadTypeSuffix<Tier, Sprite>(functionButtonConjuringTierTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, FunctionConjuringButtonSprites); }));
            // ResourceGroup.Add(CR.LoadTypeSuffix<Tier, Sprite>(functionButtonGeneratingTierTypeSuffix,
            //     result => { CR.OnTypeSuffixLoadComplete(result, FunctionGeneratingButtonSprites); }));
            ResourceGroup.Add(CR.LoadTypeSuffix<BuildingState, Sprite>(buildingStateSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, BuildingStateSprites); }));
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