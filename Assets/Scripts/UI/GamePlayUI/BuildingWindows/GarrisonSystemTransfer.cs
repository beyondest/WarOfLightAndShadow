using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public partial class GarrisonSystemTransfer : SystemBase
    {
        private InputUnitControlData _inputData;
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<GarrisonSystemConfig>();
            RequireForUpdate<InputUnitControlData>();
        }

        protected override void OnUpdate()
        {
            if(GarrisonInfoWindow.Instance == null)return;
            if (!GarrisonInfoWindow.Instance.InitGarrisonEvents)
            {
                GarrisonInfoWindow.Instance.EcsMoveOutGarrisonUnits += GarrisonMoveOutOne;
                GarrisonInfoWindow.Instance.EcsMoveOutAllGarrisonUnits += GarrisonMoveOutAll;
                GarrisonInfoWindow.Instance.InitGarrisonEvents = true;
            }
            _inputData = SystemAPI.GetSingleton<InputUnitControlData>();
        }

        private void GarrisonMoveOutAll(Entity buildingEntity)
        {
            var entity = EntityManager.CreateEntity();
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