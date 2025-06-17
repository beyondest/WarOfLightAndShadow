using System;
using UnityEngine;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.Command
{
    public class PlayerCommandSystemAuthoring : MonoBehaviour
    {
        public PlayerCommandConfig config;

        private class CommandSystemAuthoringBaker :Baker<PlayerCommandSystemAuthoring>
        {
            public override void Bake(PlayerCommandSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }

    public enum CommandType
    {
        None = 0x00,
        March = 0x01,
        Attack = 0x02,
        Harvest = 0x04,
        Garrison = 0x08,
        Heal = 0x10,
    }

    public struct CommandData : IComponentData
    {
        
    }
    
    [Serializable]
    public struct PlayerCommandConfig : IComponentData
    {
        public bool playerAlwaysFocus;
    }


    
}