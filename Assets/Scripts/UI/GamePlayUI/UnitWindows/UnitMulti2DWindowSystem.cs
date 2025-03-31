using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public partial class UnitMulti2DWindowSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
        }
        protected override void OnUpdate()
        {
            if (UnitMulti2DWindow.Instance ==null) return;
            if (!UnitMulti2DWindow.Instance.IsOpened()) return;
            
            UnitMulti2DWindow.Instance.ClearUnitInfo();
            foreach (var (unitAttr, statData, entity) in SystemAPI.Query<RefRO<UnitAttr>, RefRO<StatData>>()
                         .WithEntityAccess().WithAll<Selected>())
            {
                UnitMulti2DWindow.Instance.AddUnitInfo(new UnitMulti2DWindow.UnitInfo
                {
                    Entity = entity,
                    HpRatio = 1f-(float)statData.ValueRO.CurValue / statData.ValueRO.MaxValue,
                    UnitType = unitAttr.ValueRO.Type
                });
            }
            UnitMulti2DWindow.Instance.UpdateSelectedUnitView();
            
        }
    }
}