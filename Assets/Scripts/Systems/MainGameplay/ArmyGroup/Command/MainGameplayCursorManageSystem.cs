using System;
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
            state.RequireForUpdate<ArmyGroupSelectionData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<MainGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            ref var cursorData = ref SystemAPI.GetSingletonRW<MainGameplayCursorData>().ValueRW;
            var selectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            var hasGeneralAttr = SystemAPI.HasComponent<MainGameplayGeneralAttr>(inputMouseData.HitEntity);
            if (inputMouseData.IsOverUI || inputMouseData.HitEntity == Entity.Null ||
                selectionData.CurrentSelectCount == 0)
            {
                cursorData.CursorType = hasGeneralAttr ? MainGameplayCursorType.CheckInfo : MainGameplayCursorType.None;
                return;
            }

            if (hasGeneralAttr)
            {
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(inputMouseData.HitEntity);
                var hasSupportTag = SystemAPI.HasComponent<SupportFightTag>(inputMouseData.HitEntity);
                switch (generalAttr.BaseTag)
                {
                    case MainGameBaseTag.City:
                        if (generalAttr.Faction == playerFaction)
                        {
                            cursorData.CursorType = hasSupportTag
                                ? MainGameplayCursorType.Support
                                : MainGameplayCursorType.Garrison;
                        }
                        else
                        {
                            cursorData.CursorType = MainGameplayCursorType.Invade;
                        }
                        break;
                    case MainGameBaseTag.Army:
                        cursorData.CursorType = generalAttr.Faction == playerFaction ? MainGameplayCursorType.CheckInfo : MainGameplayCursorType.Intercept;
                        break;
                }
            }
            else if (SystemAPI.HasComponent<ArmyGroupWalkableTag>(inputMouseData.HitEntity))
            {
                cursorData.CursorType = MainGameplayCursorType.March;
            }
            else
            {
                cursorData.CursorType = MainGameplayCursorType.None;
            }
        }
    }
}