using System;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameTimeSystemAuthoring : MonoBehaviour
    {
        public GameTimeConfig config;
        private class GameTimeSystemAuthoringBaker : Baker<GameTimeSystemAuthoring>
        {
            public override void Bake(GameTimeSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
                AddComponent(entity, new WaitInfo
                {
                    WaitType = WaitType.None
                });
            }
        }
    }

    [Serializable]
    public struct GameTimeConfig : IComponentData
    {
        public WorldTimeData initWorldTimeData;
        public float gameTimeSecondToWorldTimeHour;
        public float waitTimeScale;
    }
}