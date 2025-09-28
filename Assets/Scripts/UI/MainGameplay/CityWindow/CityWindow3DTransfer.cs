using System.Collections.Generic;
using System.Linq;
using SparFlame.Components.General;
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
    public partial class CityWindow3DTransfer : SystemBase
    {
        private readonly Dictionary<Entity, CityWindow3D> _cityToCityWindow = new();

        protected override void OnCreate()
        {
            RequireForUpdate<CityAttr>();
        }


        protected override void OnUpdate()
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatus.Value == GameStatus.Init)
            {
                foreach (var slot in _cityToCityWindow.Values.Where(slot => slot != null))
                {
                    Object.Destroy(slot.gameObject);
                }
                _cityToCityWindow.Clear();
                var config = SystemAPI.ManagedAPI.GetSingleton<CityWindowConfig>();
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                foreach (var (transform, city) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CityAttr>()
                             .WithEntityAccess())
                {
                    var go = Object.Instantiate(config.Prefab);
                    var cityWindow = go.GetComponent<CityWindow3D>();
                    cityWindow.SetCity(city);
                    _cityToCityWindow.Add(city,cityWindow );
                    cityWindow.transform.position =
                        (Vector3)transform.ValueRO.Position + config.Offset;
                    cityWindow.Hide();
                    ecb.AddComponent(city, new CityWindow3DComponent
                    {
                        CityWindow3D = cityWindow
                    });
                    var armyGroups = SystemAPI.GetBuffer<CityGarrisonEntity>(city);
                    foreach (var armyGroup in armyGroups)
                    {
                        cityWindow.AddGarrison(armyGroup.ArmyGroup);
                    }
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
                return;
            }
            if(gameStatus.Value != GameStatus.MainGaming && gameStatus.Value != GameStatus.SubGaming)return;
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