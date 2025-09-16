using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Battle
{
    public class BattleRecorderSystemAuthoring : MonoBehaviour
    {
        public BattleRecorderConfig config;
        private class BattleRecorderSystemAuthoringBaker : Baker<BattleRecorderSystemAuthoring>
        {
            public override void Bake(BattleRecorderSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct BattleRecorderConfig : IComponentData
    {
        public float killedGainToLevel;
        public float destroyedGainToTier;
    }
}