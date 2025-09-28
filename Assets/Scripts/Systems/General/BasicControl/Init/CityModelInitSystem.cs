using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.Systems.General.BasicControl.TagManagement
{
    public partial struct CityModelInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CityNeedInitModelTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (cityAttr, generalAttr, children,entity) in SystemAPI
                         .Query<RefRO<CityAttr>, RefRO<MainGameplayGeneralAttr>, DynamicBuffer<Child>>().WithAll<CityNeedInitModelTag>()
                         .WithEntityAccess())
            {
                
                var darkIndex = 0;
                var lightIndex = 0;
                for (var i = 0; i < children.Length; i++)
                {
                    if (SystemAPI.HasComponent<CityDarkModelRoot>(children[i].Value))
                    {
                        darkIndex = i;
                        break;
                    }
                }
                for (int i = 0; i < children.Length; i++)
                {
                    if (SystemAPI.HasComponent<CityLightModelRoot>(children[i].Value))
                    {
                        lightIndex = i;
                        break;
                    }
                }
                var inactiveIndex = generalAttr.ValueRO.faction == FactionTag.Dark ? lightIndex : darkIndex;

                var inactiveModels = SystemAPI.GetBuffer<Child>(children[inactiveIndex].Value);
                for (var i = 0; i < inactiveModels.Length; i++)
                {
                    ecb.AddComponent<DisableRendering>(inactiveModels[i].Value);
                }
                ecb.RemoveComponent<CityNeedInitModelTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}