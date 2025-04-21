using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Exp
{
    public class ExpAttributeAuthoring : MonoBehaviour
    {
        public Tier currentTier;
        public int maxValue;        
        private class ExpAttributeAuthoringBaker : Baker<ExpAttributeAuthoring>
        {
            public override void Bake(ExpAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,new ExpData
                {
                    CurTier = authoring.currentTier,
                    MaxValue = authoring.maxValue,
                    CurValue = 0
                });
                
            }
        }
       
    }


    
    
    public struct ExpData : IComponentData
    {
        public Tier MaxTier;
        public Tier CurTier;
        public int MaxValue;
        public int CurValue;
        public Entity NextTierPrefab;
    }

    

   
    

}