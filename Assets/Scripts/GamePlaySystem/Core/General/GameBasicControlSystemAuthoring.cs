using System;
using Unity.Entities;
using UnityEngine;

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
    public enum GameStatus
    {
        NotStarted = 0, // Stay in main menu and no resource loaded
        Init = 1, // When all resource loaded, but systems not init
        SubGaming = 2, // Gaming
        Pause = 3, // Gaming pause
        MainGaming = 4,
    }
    public struct GeneralRandom : IComponentData
    {
        public Unity.Mathematics.Random Rnd;
    }
    public struct PlayerFactionData : IComponentData
    {
        public FactionTag Value;
    }

    public struct MainGamingTag : IComponentData
    {
        
    }
    // For systems that do not need game status data, require for update this one only
    public struct SubGamingTag : IComponentData
    {
       
    }

    public struct GameStatusData : IComponentData
    {
        public GameStatus Value;
    }

    public struct GameTimeData : IComponentData
    {
        public float DeltaTime;
        public float ElapsedTime;
    }

    public struct GameTimeScale : IComponentData
    {
        public float Value;
    }


}
