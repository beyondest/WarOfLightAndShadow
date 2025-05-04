using System;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class SurroundingMonitorAuthoring : MonoBehaviour
    {
        [AssetsOnly]
        public GameObject lightMonitorPrefab;
        [AssetsOnly]
        public GameObject darkMonitorPrefab;

        private class Baker : Baker<
            SurroundingMonitorAuthoring>
        {
            public override void Bake(SurroundingMonitorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MonitorPrefabData
                {
                    LightMonitorPrefab = GetEntity(authoring.lightMonitorPrefab, TransformUsageFlags.Dynamic),
                    DarkMonitorPrefab = GetEntity(authoring.darkMonitorPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }

    public struct MonitorPrefabData : IComponentData
    {
        public Entity LightMonitorPrefab;
        public Entity DarkMonitorPrefab;
    }
    public struct MonitorData : IComponentData
    {
        public Entity BelongsTo;
    }

    public struct UnderMonitorTag : IComponentData
    {
        
    }

    public struct GenerateMonitorRequest : IComponentData
    {
        public Entity TargetToMonitor;
    }
    

    public struct SurroundingValue : IComponentData
    {
        public float Value;
    }
    
    public struct SurroundingData : IBufferElementData
    {
        public Entity Entity;
    }
    
    
}