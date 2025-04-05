using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.GamePlay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BuildingSlot = SparFlame.GamePlaySystem.Building.BuildingSlot;

namespace SparFlame.GamePlaySystem.Construction
{
    public partial class ConstructSystem : SystemBase
    {
        // Internal Data
        private FactionTag _playerCurrentFaction = FactionTag.Ally; // Only work for player command
        private bool _inGhostShow;
        private ConstructSystemConfig _config;
        
        // Cache
        private NativeHashMap<int, NativeList<Entity>> _buildingDatabase;
        private InputConstructData _inputData;
        private PlacementCommandData _commandData;
        private Entity _commandEntity = Entity.Null;


        private EntityQuery _notPauseTag;
        private EntityQuery _commandDataEntityQuery;

        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<ConstructSystemConfig>();
            _commandDataEntityQuery = SystemAPI.QueryBuilder().WithAllRW<PlacementCommandData>().Build();
        }

        protected override void OnStartRunning()
        {
             _config = SystemAPI.GetSingleton<ConstructSystemConfig>();
        }

        protected override void OnUpdate()
        {
            if (!_buildingDatabase.IsCreated)
            {
                InitBuildingDatabase();
            }

            if (ConstructWindow.Instance == null || BuildingDetailWindow.Instance == null) return;
            if (!ConstructWindow.Instance.InitWindowEvents)
            {
                ConstructWindow.Instance.EcsGhostShowTargetByTypeIndex += (buildingType, saveIndex) =>
                {
                    GhostShowTargetBuilding(_buildingDatabase[(int)buildingType][saveIndex]);
                };
                ConstructWindow.Instance.EcsExitGhostShow += ExitGhostShow;
                ConstructWindow.Instance.InitWindowEvents = true;
            }

            if (!BuildingDetailWindow.Instance.InitWindowEvents)
            {
                BuildingDetailWindow.Instance.EcsGhostShowTarget += MovementGhostShowTargetBuilding;
                BuildingDetailWindow.Instance.InitWindowEvents = true;
            }
            
            // Update data
            var selectData = SystemAPI.GetSingleton<UnitSelectionData>();
            _inputData = SystemAPI.GetSingleton<InputConstructData>();
            _playerCurrentFaction = selectData.CurrentSelectFaction;
            // Check should enter construct mode by button
            CheckEnterByButton();
            // Not in construct mode, do nothing
            if (!ConstructWindow.Instance.IsOpened()) return;
            
            // Check if exit construct mode or exit ghost show
            if(CheckExit())return;
            if(CheckCancel())return;
            if (_commandDataEntityQuery.IsEmpty) return;
            // Cache the command data
            var entities = _commandDataEntityQuery.ToEntityArray(Allocator.Temp);
            var datas = _commandDataEntityQuery.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            var commandValid = false;
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _playerCurrentFaction)
                    continue;
                _commandData = datas[i];
                _commandEntity = entities[i];
                commandValid = true;
            }
            if (!commandValid) return;
            // Check construct input
            CheckBuild();
            CheckRotate();
            CheckSnap();
            EntityManager.SetComponentData(_commandEntity, _commandData);
        }

        protected override void OnDestroy()
        {
            if (_buildingDatabase.IsCreated)
            {
                foreach (var pair in _buildingDatabase)
                {
                    pair.Value.Dispose();
                }

                _buildingDatabase.Dispose();
            }
        }


        #region CheckInputMethods

        private void  CheckEnterByButton()
        {
            // Check whether already enter or no enter command
            if(!_inputData.Enter)return;
            if(ConstructWindow.Instance.IsOpened())return;
            ConstructWindow.Instance.Show();
        }
        
        private bool CheckExit()
        {
            if(!_inputData.Exit)return false;
            ExitGhostShow();
            if(ConstructWindow.Instance.IsOpened())
                ConstructWindow.Instance.Hide();
            return true;
        }
        
        private bool CheckCancel()
        {
            if (!_inputData.Cancel) return false;
            if (_inGhostShow)
            {
                ExitGhostShow();
            }
            else
            {
                if (ConstructWindow.Instance.IsOpened())
                    ConstructWindow.Instance.Hide();
            }
            return true;
        }

        private void CheckBuild()
        {
            if (!_inputData.Build) return;
            switch (_commandData.State)
            {
                case PlacementStateType.Valid:
                    _commandData.CommandType = PlacementCommandType.Build;
                    if (_commandData.IsMovementShow)
                        _inGhostShow = false; // If this is movement show mode, exit after build target
                    break;
                case PlacementStateType.Overlapping:
                    break;
                case PlacementStateType.NotEnoughResources:
                    break;
                case PlacementStateType.NotConstructable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CheckRotate()
        {
            float angle;

            if (_inputData.FineAdjustment)
            {
                if (_inputData.LeftRotate)
                    angle = -15;
                else if (_inputData.RightRotate)
                    angle = 15;
                else
                    angle = 0;
                _commandData.RotationAngle = angle;
                return;
            }

            if (math.abs(_inputData.Rotate) < 0.1f)
            {
                _commandData.RotationAngle = 0f;
                return;
            }
            angle = _inputData.Rotate * _config.RotateSpeed;
            _commandData.RotationAngle = angle;
        }

        private void CheckSnap()
        {
            // TODO : Check snap building
        }

        #endregion


        private void InitBuildingDatabase()
        {
            var buildingTypes = Enum.GetValues(typeof(BuildingType)).Length;
            _buildingDatabase =
                new NativeHashMap<int, NativeList<Entity>>(buildingTypes,
                    Allocator.Persistent);
            foreach (BuildingType buildingType in Enum.GetValues(typeof(BuildingType)))
            {
                _buildingDatabase.Add((int)buildingType, new NativeList<Entity>(Allocator.Persistent));
            }

            var buffer = SystemAPI.GetSingletonBuffer<BuildingSlot>();
            var bufferEntity = SystemAPI.GetSingletonEntity<BuildingSlot>();
            foreach (var buildingSlot in buffer)
            {
                var buildingList = _buildingDatabase[(int)buildingSlot.Type];
                buildingList.Add(buildingSlot.Entity);
            }

            EntityManager.DestroyEntity(bufferEntity);
        }


        #region GhostShow

        private void GhostShowTargetBuilding(Entity target, bool movementShow = false,
            LocalTransform oriTransform = default)
        {
            // First time enter building mode, need to create command data
            if (!_inGhostShow)
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponent<PlacementCommandData>(entity);
                var data = new PlacementCommandData
                {
                    TargetBuilding = target,
                    CommandType = PlacementCommandType.Start,
                    Faction = _playerCurrentFaction,
                    GhostModelEntity = Entity.Null,
                    GhostTriggerEntity = Entity.Null,
                    RotationAngle = 0,
                    State = PlacementStateType.Valid,
                    IsMovementShow = movementShow,
                    OriTransform = oriTransform
                };
                EntityManager.SetComponentData(entity, data);
                _inGhostShow = true;
                return;
            }

            var entities = _commandDataEntityQuery.ToEntityArray(Allocator.Temp);
            var datas = _commandDataEntityQuery.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _playerCurrentFaction)
                    continue;
                var data = datas[i];
                data.TargetBuilding = target;
                data.CommandType = PlacementCommandType.Start;
                EntityManager.SetComponentData(entities[i], data);
            }
        }

        private void MovementGhostShowTargetBuilding( Entity entity)
        {
            // Hide this entity for now, just move it to invisible place
            var transform = EntityManager.GetComponentData<LocalTransform>(entity);
            var oriTransform = transform;
            transform.Position = _config.HideBuildingLocation;
            EntityManager.SetComponentData(entity, transform);
            GhostShowTargetBuilding(entity, true, oriTransform);
        }


        private void ExitGhostShow()
        {
            var entities = _commandDataEntityQuery.ToEntityArray(Allocator.Temp);
            var datas = _commandDataEntityQuery.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _playerCurrentFaction)
                    continue;
                var data = datas[i];
                data.CommandType = PlacementCommandType.End;
                EntityManager.SetComponentData(entities[i], data);
                _inGhostShow = false;
            }
        }

        #endregion
    }
}