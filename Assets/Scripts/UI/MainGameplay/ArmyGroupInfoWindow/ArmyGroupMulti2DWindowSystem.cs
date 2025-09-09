using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.VFX;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.MainGameplay
{
    public struct ArmyGroupMulti2DRealTimeInfo
    {
        public ArmyGroupIconType IconType;
        public Entity Entity;
        public int UnitCounts;
        public int AvgLevel;
        public float TotalHpRatio;
        public List<int> UnitCountPerTier;
    }
    public partial class ArmyGroupMulti2DWindowSystem : SystemBase
    {
         private readonly List<ArmyGroupMulti2DRealTimeInfo> _infos = new();
        private bool _initEvents;

        protected override void OnCreate()
        {
            RequireForUpdate<MainGamingTag>();
        }

        protected override void OnStartRunning()
        {
            if (!_initEvents)
            {
                _initEvents = true;
                ArmyGroupMulti2DWindow.Instance.OnGetTargetEntityByIndex += index =>
                {
                    var data = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
                    UpdateSelectedInfos(data);
                    var targetEntity = index < _infos.Count ? _infos[index].Entity : Entity.Null;
                    ArmyGroupMulti2DWindow.Instance.GetSelectionData(_infos.Count, targetEntity
                    );
                    ArmyGroupMulti2DWindow.Instance.OnDeselectAllExceptOne += DeselectAllExceptOne;
                };
            }
        }

        protected override void OnUpdate()
        {
            var selectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            if (!_initEvents) return;
            if (!ArmyGroupMulti2DWindow.Instance.IsOpened()) return;
            UpdateSelectedInfos(selectionData);
        }

        private void UpdateSelectedInfos(ArmyGroupSelectionData selectionData)
        {
            _infos.Clear();
            foreach (var (armyGroupAttr,statData, entity) in SystemAPI
                         .Query<RefRO<ArmyGroupAttr>, RefRO<ArmyGroupStatData>>()
                         .WithEntityAccess().WithAll<ArmyGroupSelected>())
            {
                _infos.Add(new ArmyGroupMulti2DRealTimeInfo
                {
                    Entity = entity,
                    IconType = armyGroupAttr.ValueRO.iconType,
                    AvgLevel = armyGroupAttr.ValueRO.avgLevel,
                    UnitCounts = SystemAPI.GetBuffer<ArmyGroupUnit>(entity).Length,
                    TotalHpRatio = statData.ValueRO.totalMaxHp == 0 ? 0 : statData.ValueRO.totalCurrentHp / statData.ValueRO.totalMaxHp,
                    UnitCountPerTier = new List<int>
                    {
                        armyGroupAttr.ValueRO.tier1UnitCount,
                        armyGroupAttr.ValueRO.tier2UnitCount,
                        armyGroupAttr.ValueRO.tier3UnitCount,
                    },
                });
            }
            ArmyGroupMulti2DWindow.Instance.UpdateSelectedView(_infos, selectionData.CurrentSelectFaction);
        }
        private void DeselectAllExceptOne(Entity targetEntity)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_,entity) in SystemAPI.Query<RefRO<ArmyGroupSelected>>().WithEntityAccess())
            {
                if(entity == targetEntity)continue;
                ecb.SetComponentEnabled<ArmyGroupSelected>(entity,false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent<MainGameplayEntityTag>(vfxRequest);
                ecb.AddComponent(vfxRequest,new VFXRequest
                {
                    VFXName = VFXName.ArmyGroupSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = entity
                });
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


     
    }
}