using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.UI.General;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.GamePlay
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
        
        
        


        protected override void OnEnable()
        {
            base.OnEnable();
            var handle1 = CR.LoadTypeSuffix<UnitType, Sprite>(unitType2DSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, UnitGeneralTypeSprites));
            ResourceGroup.Add(handle1);
        }


        protected override void OnDisable()
        {
            base.OnDisable();
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