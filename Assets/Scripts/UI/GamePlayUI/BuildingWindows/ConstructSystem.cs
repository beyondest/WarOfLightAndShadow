using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
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
        private NativeHashMap<int, NativeList<Entity>> _buildingDatabase;
        
        private FactionTag _playerCurrentFaction = FactionTag.Ally;    // Only work for player command
        private bool _showGhostBuilding;
        
        private EntityQuery _notPauseTag;
        private EntityQuery _commandData;
        
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<ConstructSystemConfig>();
            _commandData = SystemAPI.QueryBuilder().WithAllRW<PlacementCommandData>().Build();
        }

        protected override void OnUpdate()
        {
            var config = SystemAPI.GetSingleton<ConstructSystemConfig>();
            if (!_buildingDatabase.IsCreated )
            {
                InitBuildingDatabase();
            }
            if(ConstructWindow.Instance == null || BuildingDetailWindow.Instance == null)return;
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
                BuildingDetailWindow.Instance.EcsGhostShowTarget += entity =>
                {
                    MovementGhostShowTargetBuilding(in config,entity);
                };
                BuildingDetailWindow.Instance.InitWindowEvents = true;
            }

            if(!ConstructWindow.Instance.IsOpened())return;
            var selectData = SystemAPI.GetSingleton<UnitSelectionData>();
            var customInput = SystemAPI.GetSingleton<CustomInputSystemData>();
            _playerCurrentFaction = selectData.CurrentSelectFaction;
            
            if(_commandData.IsEmpty)return;

            if (customInput is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left, IsOverUI: false })
            {
                var datas = _commandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
                for (var i = 0; i < datas.Length; i++)
                {
                    if (datas[i].Faction != _playerCurrentFaction) // This may happen when enemy is in ghost mode with player
                        continue;
                    var data = datas[i];
                    switch (data.State)
                    {
                        case PlacementStateType.Valid:
                            BuildTarget();
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
               
            }

            // if (Input.GetKeyDown(KeyCode.Escape))
            // {
            //     ExitGhostShow();
            // }
        }

        private void MovementGhostShowTargetBuilding(in ConstructSystemConfig config,Entity entity)
        {
            // Hide this entity for now, just move it to invisible place
             var transform = EntityManager.GetComponentData<LocalTransform>(entity);
             var oriTransform = transform;
             transform.Position = config.HideBuildingLocation;
             EntityManager.SetComponentData(entity, transform);
             GhostShowTargetBuilding(entity,true, oriTransform);
        }

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

        #region GhostShow

        private void GhostShowTargetBuilding(Entity target,bool movementShow = false,LocalTransform oriTransform = default)
        {
            // First time enter building mode, need to create command data
            if (!_showGhostBuilding)
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
                    Rotation = quaternion.identity,
                    State = PlacementStateType.Valid,
                    IsMovementShow = movementShow,
                    OriTransform = oriTransform
                };
                EntityManager.SetComponentData(entity, data);
                _showGhostBuilding = true;
                return;
            }

            var entities = _commandData.ToEntityArray(Allocator.Temp);
            var datas = _commandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
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

        private void BuildTarget()
        {
            var entities = _commandData.ToEntityArray(Allocator.Temp);
            var datas = _commandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _playerCurrentFaction)
                    continue;
                var data = datas[i];
                data.CommandType = PlacementCommandType.Build;
                EntityManager.SetComponentData(entities[i], data);
            }
        }

        private void ExitGhostShow()
        {
            var entities = _commandData.ToEntityArray(Allocator.Temp);
            var datas = _commandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _playerCurrentFaction)
                    continue;
                var data = datas[i];
                data.CommandType = PlacementCommandType.End;
                EntityManager.SetComponentData(entities[i], data);
                _showGhostBuilding = false;
            }
        }

        #endregion
    }
}