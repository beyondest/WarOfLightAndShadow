using System;
using SparFlame.Utils;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.General
{


    
    public interface IEntityPrefabData<T> : IBufferElementData where T : Enum
    {
        public Entity Prefab { get; set; }
        public T Type { get; set; }
    }

    public interface IPointsData<TData>
    {
        public int Points { get; set; }
        public TData Value { get; set; }
            
    }
    
    // TODO : Split amount range for only resource spawn data structure
    public struct ProbabilityPrefabEntry
    {
        public Entity Prefab;
        public float Probability;
        public CustomDs.Range AmountRange;
    }
    
    

   
}