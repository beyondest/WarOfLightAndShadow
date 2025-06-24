using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
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
    public partial class UnitMulti2DWindowSystem : SystemBase
    {
        private NativeList<UnitRealTimeInfo> _unitInfos;
        private bool _initEvents;

        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
            _unitInfos = new NativeList<UnitRealTimeInfo>(Allocator.Persistent);
        }

        protected override void OnStartRunning()
        {
            if (!_initEvents)
            {
                _initEvents = true;
                UnitMulti2DWindow.Instance.GetTargetEntityByIndex += index =>
                {
                    var data = SystemAPI.GetSingleton<UnitSelectionData>();
                    UpdateSelectedUnitInfos(data);
                    var targetEntity = index < _unitInfos.Length ? _unitInfos[index].Entity : Entity.Null;
                    UnitMulti2DWindow.Instance.GetUnitData(_unitInfos.Length, targetEntity
                    );
                    UnitMulti2DWindow.Instance.DeselectAllExceptOne += DeselectAllExceptOne;
                };
            }
        }

        protected override void OnUpdate()
        {
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if (!_initEvents) return;
            if (!UnitMulti2DWindow.Instance.IsOpened()) return;
            UpdateSelectedUnitInfos(unitSelectionData);
        }

        private void UpdateSelectedUnitInfos(UnitSelectionData unitSelectionData)
        {
            _unitInfos.Clear();
            foreach (var (unitAttr, statData,expData, entity) in SystemAPI
                         .Query<RefRO<UnitAttr>, RefRO<StatData>, RefRO<ExpData>>()
                         .WithEntityAccess().WithAll<Selected>())
            {
                _unitInfos.Add(new UnitRealTimeInfo
                {
                    Entity = entity,
                    HpRatio = statData.ValueRO.curValue / (statData.ValueRO.maxValue + statData.ValueRO.bonus),
                    UnitType = unitAttr.ValueRO.Type,
                    Tier = expData.ValueRO.curTier,
                    Level = expData.ValueRO.curLevel
                });
            }
            UnitMulti2DWindow.Instance.UpdateSelectedUnitView(_unitInfos, unitSelectionData.CurrentSelectFaction);
        }
        private void DeselectAllExceptOne(Entity targetEntity)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_,entity) in SystemAPI.Query<RefRO<Selected>>().WithEntityAccess())
            {
                if(entity == targetEntity)continue;
                ecb.SetComponentEnabled<Selected>(entity,false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(vfxRequest);
                ecb.AddComponent(vfxRequest,new VFXRequest
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