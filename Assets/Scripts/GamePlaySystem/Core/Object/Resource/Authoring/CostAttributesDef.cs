using System;
using Sirenix.OdinInspector;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Resource
{


    [Serializable]
    public struct CostResourceTypeAmountPair
    {
        [HideLabel]
        public ResourceType type;
        [HideLabel]
        public int amount;
    }
    
    public struct CostList : IBufferElementData
    {
        public ResourceType Type;
        public int Amount;
    }
}