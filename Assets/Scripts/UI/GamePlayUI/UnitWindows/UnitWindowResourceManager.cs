using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.UI.General;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class UnitWindowResourceManager : CustomResourceManager
    {
        
        // Config
        [Header("Resource config")]
        [SerializeField]
        [CanBeNull] private string unitType2DSpriteSuffix;
 
        // Interface
        public static UnitWindowResourceManager Instance;
        public readonly Dictionary<UnitType, Sprite> UnitSprites = new();
        private readonly Dictionary<UnitType, List<UnitInfoSpritePair>> _unitInfoSpritePairs = new();
        private readonly AddressableResourceGroup _infoWindowResourceGroup = new();
        
        private void OnEnable()
        {
            var handle1 = CR.LoadTypeSuffix<UnitType, Sprite>(unitType2DSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, UnitSprites));
            
            _infoWindowResourceGroup.Add(handle1);
        }

        

        private void OnDisable()
        {
            _infoWindowResourceGroup.Release();
            UnitSprites.Clear();
        }

        private void Awake()
        {
            if(Instance == null)    
                Instance = this;
            else
                Destroy(gameObject);
        }

        public override bool IsResourceLoaded()
        {
            return _infoWindowResourceGroup.IsHandleCreated(4)
                   && _infoWindowResourceGroup.IsDone;
        }

        private struct UnitInfoSpritePair
        {
            
        }
      
    }
}