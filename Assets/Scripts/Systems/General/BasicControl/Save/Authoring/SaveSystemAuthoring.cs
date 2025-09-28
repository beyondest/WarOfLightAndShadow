using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class SaveSystemAuthoring : MonoBehaviour
    {
        public SaveLoadConfig config;
        public bool refresh;
        private class SaveSystemAuthoringBaker : Baker<SaveSystemAuthoring>
        {
            public override void Bake(SaveSystemAuthoring authoring)
            {
                foreach (var archeTypeInfo in authoring.config.archetypes)
                {
                    archeTypeInfo.RebuildCache();
                }
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, authoring.config);
            }
        }
    }



    [Serializable]
    public class SaveLoadConfig : IComponentData
    {
        [TableList]
        public List<SaveArcheTypeInfo> archetypes;
    }
}