using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Database;

using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class UnitWindowResourceManager : TypeResourceManager<UnitType, UnitDataItem, UnitEntityPrefabData>
    {
        
        // Config
        [Header("Resource config")]
        [SerializeField]
        [CanBeNull] private string unitType2DSpriteSuffix;
 
        
        // Interface
        public static UnitWindowResourceManager Instance;
        public readonly Dictionary<UnitType, Sprite> UnitGeneralTypeSprites = new();


        public override void LoadResources()
        {
            base.LoadResources();
            var handle1 = ResourceLoadingUtils.LoadTypeSuffix<UnitType, Sprite>(unitType2DSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, UnitGeneralTypeSprites));
            ResourceGroup.Add(handle1);
        }


        public override void UnloadResources()
        {
            base.UnloadResources();
            UnitGeneralTypeSprites.Clear();
        }

        private void Awake()
        {
            if(Instance == null)    
                Instance = this;
            else
                Destroy(gameObject);
        }
    }
}