using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.GamePlay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BuildingSlot = SparFlame.GamePlaySystem.Building.BuildingSlot;

namespace SparFlame.GamePlaySystem.Construction
{
    public partial class ConstructSystem : SystemBase
    {
        private NativeHashMap<int, NativeList<Entity>> _buildingDatabase;
        
        private FactionTag _playerCurrentFaction;    // Only work for player command
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
            if(ConstructWindow.Instance == null)return;
            if (!ConstructWindow.Instance.InitWindowEvents)
            {
                ConstructWindow.Instance.EcsGhostShowTarget += (buildingType, saveIndex) =>
                {
                    GhostShowTargetBuilding(_buildingDatabase[(int)buildingType][saveIndex]);
                };
                ConstructWindow.Instance.EcsBuildTarget += BuildTarget;
                ConstructWindow.Instance.EcsExitGhostShow += ExitGhostShow;
                ConstructWindow.Instance.InitWindowEvents = true;
            }
            
            if(!ConstructWindow.Instance.IsOpened())return;
            var selectData = SystemAPI.GetSingleton<UnitSelectionData>();
            var customInput = SystemAPI.GetSingleton<CustomInputSystemData>();
            _playerCurrentFaction = selectData.CurrentSelectFaction;
            
            if(_commandData.IsEmpty)return;
            
            if (customInput is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left, IsOverUI: false })
            {
                var datas = _commandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
                var data = new PlacementCommandData();
                for (var i = 0; i < datas.Length; i++)
                {
                    if (datas[i].Faction != _playerCurrentFaction) // This may happen when enemy is in ghost mode with player
                        continue;
                    data = datas[i];
                }
                switch (data.State)
                {
                    case PlacementStateType.Valid:
                        BuildTarget();
                        break;
                    case PlacementStateType.Overlapping:
                    case PlacementStateType.NotEnoughResources:
                    case PlacementStateType.NotConstructable:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitGhostShow();
            }
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
                _buildingDatabase.Dispose();
            }
        }

        #region GhostShow

        private void EnterGhostShow(Entity target)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponent<PlacementCommandData>(entity);
            var data = new PlacementCommandData
            {
                TargetBuilding = target,
                CommandType = PlacementCommandType.Start,
                Faction = _playerCurrentFaction,
                GhostEntity = Entity.Null,
                GhostTriggerEntity = Entity.Null,
                Rotation = quaternion.identity,
                State = PlacementStateType.Valid
            };
            EntityManager.SetComponentData(entity, data);
        }

        private void GhostShowTargetBuilding(Entity target)
        {
            if (!_showGhostBuilding)
            {
                EnterGhostShow(target);
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