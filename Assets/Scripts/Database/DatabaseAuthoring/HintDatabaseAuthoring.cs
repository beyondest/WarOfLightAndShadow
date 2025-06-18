using System;
using System.Collections.Generic;
using System.Linq;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class HintDatabaseAuthoring : MonoBehaviour
    {
        private class HintDatabaseAuthoringBaker : Baker<HintDatabaseAuthoring>
        {
            public override void Bake(HintDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<HintConfigs>(entity);
                var have = new HashSet<HintName>();
                foreach (var item in DatabaseManager.HintDatabaseSo.items)
                {
                    if(!have.Add(item.name))
                        throw new ArgumentException("Same hint type already config in hint database");
                    buffer.Add(new HintConfigs
                    {
                        Content = item.content,
                        Name = item.name,
                        Type = item.hintType
                    });
                }

                var all = new HashSet<HintName>();
                foreach (HintName type in Enum.GetValues(typeof(HintName)))
                {
                    all.Add(type);
                }

                foreach (var type in all.Except(have))
                {
                    buffer.Add(new HintConfigs
                    {
                        Content = Enum.GetName(typeof(HintName), type),
                        Name = type,
                    });
                }
            }
        }
    }
}