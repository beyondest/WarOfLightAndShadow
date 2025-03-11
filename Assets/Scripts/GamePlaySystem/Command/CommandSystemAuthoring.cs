using UnityEngine;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.Command
{
    public class CommandSystemAuthoring : MonoBehaviour
    {
        private class CommandSystemAuthoringBaker :Baker<CommandSystemAuthoring>
        {
            public override void Bake(CommandSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CommandConfig
                {

                });
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
    
    public struct CommandConfig : IComponentData
    {
        
    }


    
}