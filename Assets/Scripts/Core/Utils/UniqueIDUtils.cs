using System;
using Unity.Entities;

namespace SparFlame.Core.Utils
{
    
    
    [Serializable]
    public struct LastUniqueId : IComponentData
    {
        public int value;
    }

    [Serializable]
    public struct CityTaskUniqueId : IComponentData
    {
        public int value;
    }
    
    public static class UniqueIDUtils
    {
        public static int GetUniqueId(ref LastUniqueId lastUniqueId)
        {
            return ++lastUniqueId.value;
        }
        
      
    }
}