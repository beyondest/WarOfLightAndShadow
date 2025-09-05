using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class SaveSystemAuthoring : MonoBehaviour
    {
        private class SaveSystemAuthoringBaker : Baker<SaveSystemAuthoring>
        {
            public override void Bake(SaveSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new SaveLoadConfig
                {
                });
      
              
            }
        }
    }


    public struct SaveLoadConfig : IComponentData
    {
    }
    public enum SaveLoadType
    {
        None = 0,
        OnlyCity = 1,
        OnlyMainGameplay = 2,
        AllFromCity = 3,
        AllFromWildBattle = 4,
    }
   

    
    
}