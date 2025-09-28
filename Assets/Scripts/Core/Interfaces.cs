using System;
using Unity.Entities;

namespace SparFlame.Core.Interfaces
{
    public interface IEntityPrefabData<T> : IBufferElementData where T : Enum
    {
        public Entity Prefab { get; set; }
        public T Type { get; set; }
        public int PrefabId { get; set; }
    }

    public interface IPointsData<TData>
    {
        public int Points { get; set; }
        public TData Value { get; set; }
            
    }
    
    
    /// <summary>
    /// Every IResourceManager should be put in bootStrapper scene,
    /// and if it is to be destroyed, you have to manually release resource and unregister
    /// </summary>
    public interface IResourceManager
    {
        bool IsInitialized { get; }
        float InitProgress { get; }
        void LoadResources();
        void UnloadResources();
    }
    
}