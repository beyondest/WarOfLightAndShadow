using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.SubGameplay;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public partial class GarrisonSystemTransfer : SystemBase
    {
        private InputUnitControlData _inputData;
        private bool _initEvents;
        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
            RequireForUpdate<GarrisonSystemConfig>();
            RequireForUpdate<InputUnitControlData>();
        }

        protected override void OnStartRunning()
        {
            if (!_initEvents)
            {
                _initEvents = true;
                SubGameplayGarrisonInfoWindow.Instance.EcsMoveOutGarrisonUnits += GarrisonMoveOutOne;
                SubGameplayGarrisonInfoWindow.Instance.EcsMoveOutAllGarrisonUnits += GarrisonMoveOutAll;
            }
        }

        protected override void OnUpdate()
        {
            _inputData = SystemAPI.GetSingleton<InputUnitControlData>();
        }

        private void GarrisonMoveOutAll(Entity buildingEntity)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponent<SubGameplayEntityTag>(entity);
            EntityManager.AddComponent<GarrisonMoveOutCommand>(entity);
            var command = new GarrisonMoveOutCommand
            {
                BuildingEntity = buildingEntity,
                MoveOutUnitId = 0,
                MoveOutAllSameId = _inputData.MoveOutSameIdUnits,
                MoveOutAll = true
            };
            EntityManager.SetComponentData(entity, command);
        }
        private void GarrisonMoveOutOne(int moveOutUnitsId,Entity buildingEntity)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponent<SubGameplayEntityTag>(entity);

            EntityManager.AddComponent<GarrisonMoveOutCommand>(entity);
            var command = new GarrisonMoveOutCommand
            {
                BuildingEntity = buildingEntity,
                MoveOutUnitId = moveOutUnitsId,
                MoveOutAllSameId = _inputData.MoveOutSameIdUnits,
                MoveOutAll = false
            };
            EntityManager.SetComponentData(entity, command);
        }
    }
}