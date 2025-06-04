using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public partial class GarrisonSystemTransfer : SystemBase
    {
        private InputUnitControlData _inputData;
        private bool _initEvents;
        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<GarrisonSystemConfig>();
            RequireForUpdate<InputUnitControlData>();
        }

        protected override void OnStartRunning()
        {
            if (!_initEvents)
            {
                _initEvents = true;
                GarrisonInfoWindow.Instance.EcsMoveOutGarrisonUnits += GarrisonMoveOutOne;
                GarrisonInfoWindow.Instance.EcsMoveOutAllGarrisonUnits += GarrisonMoveOutAll;
            }
        }

        protected override void OnUpdate()
        {
            _inputData = SystemAPI.GetSingleton<InputUnitControlData>();
        }

        private void GarrisonMoveOutAll(Entity buildingEntity)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponent<GameplayEntityTag>(entity);
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
            EntityManager.AddComponent<GameplayEntityTag>(entity);

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