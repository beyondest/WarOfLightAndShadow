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
                AddComponent(entity,new SaveConfig
                {
                });
                AddComponent(entity, new SaveData
                {
                    Type = SaveLoadType.None,
                    CityId = -1,
                });
                AddComponent(entity, new LoadData
                {
                    Type = SaveLoadType.None,
                    CityId = -1,
                });
            }
        }
    }


    public struct SaveConfig : IComponentData
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
    public struct SaveData : IComponentData
    {
        public int CityId;
        public SaveLoadType Type;
    }
    public struct LoadData : IComponentData
    {
        public int CityId;
        public SaveLoadType Type;
    }
    
    
}