using Unity.Entities;

namespace SparFlame.Components.General
{
    public enum BattleFieldType
    {
        None = 0,
        Prairie = 1,
        Forest = 2,
    }
    
 
    
    public struct BattleTriggerRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Defender;
        public SubGameStatus TargetSubGameStatus;
    }
    public struct SupportFightTag : IComponentData{}

    public struct SupportFightInvader : IBufferElementData
    {
        public Entity Entity;
    }
    public struct BattleUtils
    {
        public static void BeginBattle(SubGameStatus targetSubGameStatus, 
            Entity attacker,
            Entity defender,
            int index,
            EntityCommandBuffer.ParallelWriter ecb)
        {
            var warRequest = ecb.CreateEntity(index);
            ecb.AddComponent<MainGameplayEntityTag>(index, warRequest);
            
            ecb.AddComponent(index, warRequest, new BattleTriggerRequest
            {
                Attacker = attacker,
                Defender = defender,
                TargetSubGameStatus = targetSubGameStatus
            });
        }
    }
}