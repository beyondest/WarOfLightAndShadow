using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Database;

using SparFlame.GamePlaySystem.Units;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
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
            var handle1 = CR.LoadTypeSuffix<UnitType, Sprite>(unitType2DSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, UnitGeneralTypeSprites));
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