using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct MainGameplayCursorManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<MainGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inputData = SystemAPI.GetSingleton<InputMouseData>();
            ref var cursorData = ref SystemAPI.GetSingletonRW<MainGameplayCursorData>().ValueRW;
            if (inputData.IsOverUI || inputData.HitEntity == Entity.Null)
            {
                cursorData.Type = MainGameplayCursorType.None;
                return;
            }

            if (SystemAPI.HasComponent<MainGameplayGeneralAttr>(inputData.HitEntity))
            {
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(inputData.HitEntity);
                cursorData.Type = generalAttr.BaseTag switch
                {
                    MainGameBaseTag.City => MainGameplayCursorType.City,
                    MainGameBaseTag.Army => MainGameplayCursorType.ArmyGroup,
                    _ => MainGameplayCursorType.None
                };
            }
            else if (SystemAPI.HasComponent<ArmyGroupWalkableTag>(inputData.HitEntity))
            {
                cursorData.Type = MainGameplayCursorType.March;
            }
            else
            {
                cursorData.Type = MainGameplayCursorType.None;
            }
        }

       
    }
}