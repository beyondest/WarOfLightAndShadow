using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Interfaces;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public struct UnitRealTimeInfo
    {
        public UnitType UnitType;
        public float HpRatio;
        public Entity Entity;
        public Tier Tier;
        public int Level;
    }

    public partial class UnitMulti2DWindowSystem : SystemBase, IEcsTransferSystem<UnitMulti2DWindow>
    {
        private NativeList<UnitRealTimeInfo> _unitInfos;
        private bool _initEvents;
        private UnitMulti2DWindow _mono;

        public void Init(UnitMulti2DWindow mono)
        {
            _initEvents = true;
            _mono = mono;
            _mono.OnGetTargetEntityByIndex += index =>
            {
                var data = SystemAPI.GetSingleton<UnitSelectionData>();
                UpdateSelectedUnitInfos(data);
                var targetEntity = index < _unitInfos.Length ? _unitInfos[index].Entity : Entity.Null;
                _mono.GetUnitData(_unitInfos.Length, targetEntity
                );
                _mono.OnDeselectAllExceptOne += DeselectAllExceptOne;
            };
        }

        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
            _unitInfos = new NativeList<UnitRealTimeInfo>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            if (!_initEvents) return;
            if (!_mono.IsOpened()) return;
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            UpdateSelectedUnitInfos(unitSelectionData);
        }

        private void UpdateSelectedUnitInfos(UnitSelectionData unitSelectionData)
        {
            _unitInfos.Clear();
            foreach (var (unitAttr, statData, expData, entity) in SystemAPI
                         .Query<RefRO<UnitAttr>, RefRO<StatData>, RefRO<ExpData>>()
                         .WithEntityAccess().WithAll<Selected>())
            {
                var countLevel = expData.ValueRO.curLevel + ((int)expData.ValueRO.curTier - 3) * 10;
                _unitInfos.Add(new UnitRealTimeInfo
                {
                    Entity = entity,
                    HpRatio = statData.ValueRO.curValue / statData.ValueRO.maxValue /*+ statData.ValueRO.bonus*/,
                    UnitType = unitAttr.ValueRO.type,
                    Tier = expData.ValueRO.curTier,
                    Level = countLevel
                });
            }
            _mono.UpdateSelectedUnitView(_unitInfos, unitSelectionData.CurrentSelectFaction);
        }

        private void DeselectAllExceptOne(Entity targetEntity)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<Selected>>().WithEntityAccess())
            {
                if (entity == targetEntity) continue;
                ecb.SetComponentEnabled<Selected>(entity, false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(vfxRequest);
                ecb.AddComponent(vfxRequest, new VFXRequest
                {
                    VFXName = VFXName.UnitSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = entity
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


        protected override void OnDestroy()
        {
            if (_unitInfos.IsCreated)
                _unitInfos.Dispose();
        }
    }
}