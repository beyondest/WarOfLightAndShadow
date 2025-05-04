using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.General
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
                AddComponent(notDestroyEntity, new GameStatusData
                {
                    Value = GameStatus.NotStarted
                });
            }
        }
    }

    [Serializable]
    public struct GameBasicConfig : IComponentData
    {
        public bool enablePause;
        public int targetFrameRate;
    }
    public enum GameStatus
    {
        NotStarted = 0, // Stay in main menu and no resource loaded
        Init = 1, // When all resource loaded, but systems not init
        Gaming = 2, // Gaming
        Pause = 3 // Gaming pause
    }
    public struct GeneralRandom : IComponentData
    {
        public Unity.Mathematics.Random Rnd;
    }
    public struct PlayerFactionData : IComponentData
    {
        public FactionTag Value;
    }
    public struct GameStartTime : IComponentData
    {
        public float Value;
    }

    // For systems that do not need game status data, require for update this one only
    public struct GamingTag : IComponentData
    {
       
    }

    public struct GameStatusData : IComponentData
    {
        public GameStatus Value;
    }

    


}
