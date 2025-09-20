using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.Battle
{
    public partial struct RetreatPortalManageSystem : ISystem
    {
        public struct RetreatPortalHideTag : IComponentData
        {
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<SubGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (GameStatusUtils.IsInBattle(subGameStatusData))
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<RetreatPortalTag>>().WithAll<RetreatPortalHideTag>()
                             .WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
                {
                    ecb.RemoveComponent<RetreatPortalHideTag>(entity);
                    ecb.RemoveComponent<Disabled>(entity);
                }
            }
            else
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<RetreatPortalTag>>().WithNone<RetreatPortalHideTag>()
                             .WithEntityAccess())
                {
                    ecb.AddComponent<RetreatPortalHideTag>(entity);
                    ecb.AddComponent<Disabled>(entity);
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

     
    }
}