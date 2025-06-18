using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameBasicControlSystemAuthoring : MonoBehaviour
    {
        public GameBasicConfig config;

        class Baker : Baker<GameBasicControlSystemAuthoring>
        {
            public override void Bake(GameBasicControlSystemAuthoring authoring)
            {
                var notDestroyEntity = GetEntity(TransformUsageFlags.None);
                AddComponent(notDestroyEntity, authoring.config);
                
            }
        }
    }

    [Serializable]
    public struct GameBasicConfig : IComponentData
    {
        public bool enablePause;
        public int targetFrameRate;
        [Tooltip("Default should be 1/60")]
        public float basicFixStep;
    }

    public enum GameStatusSwitchType
    {
        SubGameToMainGame,
        MainGameToSubGame,
    }



}
