using System;
using System.Collections.Generic;
using System.Linq;
using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class EnvTypeSpawnDatabaseAuthoring : MonoBehaviour
    {
        private class EnvDatabaseAuthoringBaker : Baker<EnvTypeSpawnDatabaseAuthoring>
        {
            public override void Bake(EnvTypeSpawnDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<EnvSpawnTypeTotalAmount>(entity);
                var set = new HashSet<EnvType>();
                var items = DatabaseManager.EnvTypeSpawnDatabaseSo.items;
                foreach (var item in items)
                {
                    if (!set.Add(item.type))
                        throw new ArgumentException("Env spawn config wrong, duplicated env type ");
                }
                
                if (items.Count != Enum.GetValues(typeof(EnvType)).Length)
                {
                    var totalSet = new HashSet<EnvType>();
                    foreach (EnvType type in Enum.GetValues(typeof(EnvType)))
                    {
                        totalSet.Add(type);
                    }
                
                    var e = totalSet.Except(set);
                    foreach (var t in e)
                    {
                        Debug.Log($"Miss : {t}");
                    }
                    throw new ArgumentException("Env spawn config wrong, Not enough env type, ");
                
                }
                
                var buffer2 = AddBuffer<EnvTileTypeSpecialData>(entity);
                foreach (var item in items)
                {
                    var fix = new FixedList128Bytes<TileTypeToSpawnWeight>();
                    foreach (var weight in item.spawnableTiles)
                    {
                        fix.Add(weight);
                    }
                    buffer.Add(new EnvSpawnTypeTotalAmount
                    {
                        Amount = item.amount,
                        Type = item.type
                    });
                    buffer2.Add(new EnvTileTypeSpecialData
                    {
                        type = item.type,
                        spawnableTiles = fix
                    });
                }
            }
        }
    }
}