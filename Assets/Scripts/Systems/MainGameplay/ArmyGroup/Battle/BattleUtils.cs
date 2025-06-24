using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Entities;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
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