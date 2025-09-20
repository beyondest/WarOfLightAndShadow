using System.Collections.Generic;
using SparFlame.Components.MainGameplay;
using SparFlame.UI.MainGameplay;
using SparFlame.UI.MainGameplay.CityWindow;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{

    public class CityWindow3DComponent : IComponentData
    {
        public CityWindow3D CityWindow3D;
    }
    [UpdateBefore(typeof(ArmyGroupGarrisonSystem))]
    public partial class ArmyGroupGarrisonSystemTransfer : SystemBase
    {
        private readonly Dictionary<Entity, CityWindow3D> _cityToCityWindow = new();

        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<CityAttr>();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                var config = SystemAPI.ManagedAPI.GetSingleton<CityWindowConfig>();
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                _initialized = true;
                foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CityAttr>()
                             .WithEntityAccess())
                {
                    var go = Object.Instantiate(config.Prefab);
                    var cityWindow = go.GetComponent<CityWindow3D>();
                    cityWindow.SetCity(entity);
                    _cityToCityWindow.Add(entity,cityWindow );
                    cityWindow.transform.position =
                        (Vector3)transform.ValueRO.Position + config.Offset;
                    cityWindow.Hide();
                    ecb.AddComponent(entity, new CityWindow3DComponent
                    {
                        CityWindow3D = cityWindow
                    });
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            foreach (var request in SystemAPI.Query<RefRO<ArmyGroupGarrisonRequest>>())
            {
                var window = _cityToCityWindow[request.ValueRO.City];
                if (request.ValueRO.IfGarrisonIn)
                {
                    window.AddGarrison(request.ValueRO.ArmyGroup);
                }
                else
                {
                    window.RemoveGarrison(request.ValueRO.ArmyGroup);
                }
            }
        }
    }
}