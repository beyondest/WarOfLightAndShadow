using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;

namespace SparFlame.System.Spawn
{
    public class PrefabInitAuthoring : MonoBehaviour
    {
        public List<int> disableChildIndices;
        class Baker : Baker<PrefabInitAuthoring>
        {
            public override void Bake(PrefabInitAuthoring authoring)
            {
                if(authoring.disableChildIndices.Count != authoring.disableChildIndices.Distinct().Count())
                    throw new ArgumentException("disableChildIndices can not duplicate");
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<PrefabDisableChildIndices>(entity);
                foreach (var value in authoring.disableChildIndices)
                {
                    buffer.Add(new PrefabDisableChildIndices{Value = value});
#if DEBUG
                    Debug.Log($"Initial disable prefab child index: {value}");               
#endif
                }
            }
        }
    }
    public struct PrefabDisableChildIndices : IBufferElementData
    {
        public int Value;
    }
}