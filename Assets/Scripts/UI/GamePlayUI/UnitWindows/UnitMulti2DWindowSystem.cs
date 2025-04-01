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
            if (!_isInitialized && UnitMulti2DWindow.Instance != null)
            {
                UnitMulti2DWindow.Instance.GetTargetEntityByIndex += index =>
                {
                    var targetEntity = index < _unitInfos.Length ? _unitInfos[index].Entity : Entity.Null;
                    UnitMulti2DWindow.Instance.GetUnitData(_unitInfos.Length,targetEntity);
                };
                _unitInfos = new NativeList<UnitRealTimeInfo>(Allocator.Persistent);
                _isInitialized = true;
            }
            if (!_isInitialized) return;
            if (!UnitMulti2DWindow.Instance.IsOpened()) return;
            _unitInfos.Clear();
            foreach (var (unitAttr, statData, entity) in SystemAPI.Query<RefRO<UnitAttr>, RefRO<StatData>>()
                         .WithEntityAccess().WithAll<Selected>())
            {
                _unitInfos.Add(new UnitRealTimeInfo
                {
                    Entity = entity,
                    HpRatio = 1f-(float)statData.ValueRO.CurValue / statData.ValueRO.MaxValue,
                    UnitType = unitAttr.ValueRO.Type
                });
            }
            UnitMulti2DWindow.Instance.UpdateSelectedUnitView(_unitInfos);
        }


        protected override void OnDestroy()
        {
            if(_unitInfos.IsCreated)
                _unitInfos.Dispose();
        }
    }
    public struct UnitRealTimeInfo
    {
        public UnitType UnitType;
        public float HpRatio;
        public Entity Entity;
    }
    
    
}