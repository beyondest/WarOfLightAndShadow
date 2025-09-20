using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public class EnemyCheckFocusPlayerSystemAuthoring : MonoBehaviour
    {
        public EnemyCheckFocusPlayerConfig config;
        private class
            EnemyCheckFocusPlayerSystemAuthoringBaker : Baker<EnemyCheckFocusPlayerSystemAuthoring>
        {
            public override void Bake(EnemyCheckFocusPlayerSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct EnemyCheckFocusPlayerConfig : IComponentData
    {
        public int countThresholdToFocusPlayer;
    }
}