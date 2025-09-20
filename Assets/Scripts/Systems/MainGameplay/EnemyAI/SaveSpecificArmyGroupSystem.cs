using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using Unity.Collections;
using Unity.Entities;
// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public partial class SaveSpecificArmyGroupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnemyArmyGroupShouldSaveTag>();
        }

        protected override void OnUpdate()
        {
            var find = false;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (units, armyGroup) in SystemAPI.Query<DynamicBuffer<ArmyGroupUnit>>()
                         .WithAll<EnemyArmyGroupShouldSaveTag>().WithEntityAccess())
            {
                if(units.Length == 0)
                    continue;
                find = true;
                ecb.AddComponent<EnemyArmyGroupSaveTag>(armyGroup);
                ecb.RemoveComponent<EnemyArmyGroupShouldSaveTag>(armyGroup);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
            if (find)
            {
                SaveLoadController.Instance.SyncSaveEnemySpecificArmyGroupSubData();
            }
        }
    }
}