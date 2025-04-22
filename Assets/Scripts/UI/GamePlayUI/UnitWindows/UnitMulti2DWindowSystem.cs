using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public partial class UnitMulti2DWindowSystem : SystemBase
    {
        private NativeList<UnitRealTimeInfo> _unitInfos;
        private bool _isInitialized;

        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
        }


        protected override void OnUpdate()
        {
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if (!_isInitialized && UnitMulti2DWindow.Instance != null)
            {
                UnitMulti2DWindow.Instance.GetTargetEntityByIndex += index =>
                {
                    var data = SystemAPI.GetSingleton<UnitSelectionData>();
                    UpdateSelectedUnitInfos(data);
                    var targetEntity = index < _unitInfos.Length ? _unitInfos[index].Entity : Entity.Null;
                    UnitMulti2DWindow.Instance.GetUnitData(_unitInfos.Length, targetEntity
                        );
                };
                _unitInfos = new NativeList<UnitRealTimeInfo>(Allocator.Persistent);
                _isInitialized = true;
            }

            if (!_isInitialized) return;
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
                    HpRatio = statData.ValueRO.CurValue / statData.ValueRO.MaxValue,
                    UnitType = unitAttr.ValueRO.Type,
                    Tier = expData.ValueRO.CurTier
                });
            }
            UnitMulti2DWindow.Instance.UpdateSelectedUnitView(_unitInfos, unitSelectionData.CurrentSelectFaction);
        }


        protected override void OnDestroy()
        {
            if (_unitInfos.IsCreated)
                _unitInfos.Dispose();
        }
    }

    public struct UnitRealTimeInfo
    {
        public UnitType UnitType;
        public float HpRatio;
        public Entity Entity;
        public Tier Tier;
    }
}