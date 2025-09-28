using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.BasicControl.GlobalMonos;
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
            var shouldSaveArmyGroupHasUnits = false;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (units,shouldSaveTag, armyGroup) in SystemAPI.Query<DynamicBuffer<ArmyGroupUnit>,
                         RefRO<EnemyArmyGroupShouldSaveTag>>()
                         .WithEntityAccess())
            {
                if (units.Length != shouldSaveTag.ValueRO.TotalUnitCount)
                    continue;
                shouldSaveArmyGroupHasUnits = true;
                ecb.AddComponent<EnemyArmyGroupSaveTag>(armyGroup);
                ecb.RemoveComponent<EnemyArmyGroupShouldSaveTag>(armyGroup);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
            if (!shouldSaveArmyGroupHasUnits) return;
            CustomCoroutineRunner.Instance.StartCoroutine(
                SaveLoadController.Instance.SaveAsync(SaveType.SaveEnemySpecificArmyGroupSubData, -1));
        }
    }
}