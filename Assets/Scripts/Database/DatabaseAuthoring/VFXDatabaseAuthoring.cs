using System;
using System.Collections.Generic;
using SparFlame.Components.VFX;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class VFXDatabaseAuthoring : MonoBehaviour
    {
        private class VFXDatabaseAuthoringBaker : Baker<VFXDatabaseAuthoring>
        {
            public override void Bake(VFXDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<VFXConfigData>(entity);
                var items = DatabaseManager.VFXDatabaseSo.items;
                var set = new HashSet<VFXName>();

                foreach (var item in items)
                {
                    set.Add(item.name);
                    
                    buffer.Add(new VFXConfigData
                    {
                        Name = item.name,
                        Pair = new VFXPrefabDataPair
                        {
                            Prefab = GetEntity(item.prefab, TransformUsageFlags.Dynamic),
                            // SubIndex = item.hasFaction ? item.subIndex : 0,
                            VFXType = item.type,
                            PData = new ProjectileInitData
                            {
                                HitEffectName = item.hitEffectName,
                                HorizontalSpeed = item.horizontalSpeed,
                                BaseRelativeHeight = item.baseRelativeHeight,
                                MaxFlightDistance = item.maxFlightDistance,
                                ProjectileType = item.projectileType,
                                InitialHeight = item.initialHeight,
                            },
                            Filter = new VFXSubFilter
                            {
                                FactionFilterEnable = item.hasFaction,
                                TierFilterEnable = item.hasTier,
                                Faction = item.faction,
                                Tier = item.tier
                            },
                            KillUntilAllStopPlay = item.killUntilAllStopPlay,
                            MaxWaitTimeForAllStopPlay = item.maxWaitTimeForAllStopPlay
                        }
                    });
                }

                var full = new HashSet<VFXName>();
                foreach (VFXName name in Enum.GetValues(typeof(VFXName)))
                {
                    full.Add(name);
                }

                // if (set.Count != full.Count)
                // {
                //     foreach (var name in full.Except(set))
                //     {
                //         Debug.LogError($"VFX Miss name : {name} ");
                //     }
                // }
            }
        }
    }
}