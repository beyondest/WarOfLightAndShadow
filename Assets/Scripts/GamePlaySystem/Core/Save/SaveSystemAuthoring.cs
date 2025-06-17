using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Save
{
    public class SaveSystemAuthoring : MonoBehaviour
    {
        public string savePath;
        private class SaveSystemAuthoringBaker : Baker<SaveSystemAuthoring>
        {
            public override void Bake(SaveSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new SaveConfig
                {
                    SavePath = authoring.savePath,
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
        public FixedString32Bytes SavePath;
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