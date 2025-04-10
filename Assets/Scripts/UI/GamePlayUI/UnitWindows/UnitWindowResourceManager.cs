﻿using System;
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
        public readonly Dictionary<InteractType, List<Sprite>> InteractAbilitySprites = new();
        
        
        private readonly AddressableResourceGroup _infoWindowResourceGroup = new();

        private void OnEnable()
        {
            CR.LoadEnumProperties<InteractType,IInteractAbility,Sprite>(_infoWindowResourceGroup, InteractAbilitySprites,1);
            var handle1 = CR.LoadTypeSuffix<UnitType, Sprite>(unitType2DSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, UnitSprites));
            
            _infoWindowResourceGroup.Add(handle1);
        }

        

        private void OnDisable()
        {
            _infoWindowResourceGroup.Release();
            UnitSprites.Clear();
            InteractAbilitySprites.Clear();
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
        
        private void LoadEnumAndPropertyResource()
        {
            var interactProperties = typeof(IInteractAbility).GetProperties();
            var propertyNames = new List<string>();
            foreach (var propertyInfo in interactProperties)
            {
                propertyNames.Add(propertyInfo.Name);
            }
            
            foreach (InteractType type in Enum.GetValues(typeof(InteractType)))
            {
                var spriteList = new List<Sprite>();
                var prefix = Enum.GetName(typeof(InteractType), type);
                var keys = new List<string>();
                foreach (var propertyName in propertyNames)
                {
                    keys.Add(prefix + propertyName);
                }
                var handle = CR.LoadAssetsNameAsync<Sprite> (keys, null, result =>
                {
                    spriteList.AddRange(result);
                    InteractAbilitySprites.Add(type, spriteList);
                });
                _infoWindowResourceGroup.Add(handle);
            }
        }
    }
}