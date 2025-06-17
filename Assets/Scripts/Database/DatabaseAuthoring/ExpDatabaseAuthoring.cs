using SparFlame.GamePlaySystem.Interact;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class ExpDatabaseAuthoring : MonoBehaviour
    {
        private class ExpDatabaseAuthoringBaker : Baker<ExpDatabaseAuthoring>
        {
            public override void Bake(ExpDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ExpStaticConfig>(entity);
                foreach (var item in DatabaseManager.UnitDatabaseSo.Items)
                {
                    buffer.Add(new ExpStaticConfig
                    {
                        GlobalIdx = item.id,
                        MaxTier = item.maxTier,
                        NextTierPrefab = !item.nextTierPrefab
                            ? Entity.Null
                            : GetEntity(item.nextTierPrefab, TransformUsageFlags.Dynamic),
                        MaxLevel = item.maxLevel,
                        StatPerLevel = item.statPerLevel,
                        ExpGainPerLevel = item.expGainPerLevel,
                        MoveSpeedPerLevel = item.moveSpeedPerLevel,
                        AttackAmountPerLevel = item.attackAmountPerLevel,
                        AttackRangePerLevel = item.attackRangePerLevel,
                        AttackSpeedPerLevel = item.attackSpeedPerLevel,
                        AttackTargetsPerLevel = item.attackTargetsPerLevel,
                        HealAmountPerLevel = item.healAmountPerLevel,
                        HealRangePerLevel = item.healRangePerLevel,
                        HealTargetsPerLevel = item.healTargetsPerLevel,
                        HealSpeedPerLevel = item.healSpeedPerLevel,
                        HarvestAmountPerLevel = item.harvestAmountPerLevel,
                        HarvestRangePerLevel = item.harvestRangePerLevel,
                        HarvestTargetsPerLevel = item.harvestTargetsPerLevel,
                        HarvestSpeedPerLevel = item.harvestSpeedPerLevel,
                    });
                }

                foreach (var item in DatabaseManager.BuildingDatabaseSo.Items)
                {
                    buffer.Add(new ExpStaticConfig
                    {
                        GlobalIdx = item.id,
                        MaxTier = item.maxTier,
                        NextTierPrefab = !item.nextTierPrefab
                            ? Entity.Null
                            : GetEntity(item.nextTierPrefab, TransformUsageFlags.Dynamic),
                        MaxLevel = item.maxLevel,
                        StatPerLevel = item.statPerLevel,
                        ExpGainPerLevel = item.expGainPerLevel,
                    });
                }
            }
        }
    }
}