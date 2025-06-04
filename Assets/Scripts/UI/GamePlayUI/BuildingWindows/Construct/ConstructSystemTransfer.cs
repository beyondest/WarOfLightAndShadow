using System;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.GamePlay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Construction
{
    public partial class ConstructSystemTransfer : SystemBase
    {
        // Internal Data
        private FactionTag _playerCurrentFaction = FactionTag.Ally; // Only work for player command
        private bool _inGhostShow;
        private ConstructSystemConfig _config;

        private bool _initEvent;

        // Cache
        // private NativeHashMap<int, NativeList<Entity>> _buildingDatabase; // (int)BuildingType to building entity prefab list
        private InputConstructData _inputData;


        private EntityQuery _gamingTag;

        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<ConstructSystemConfig>();
            RequireForUpdate<ConstructCommandData>();
        }

        protected override void OnStartRunning()
        {
            // When gameStatus is gaming, instance can never be null
            if (!_initEvent)
            {
                _config = SystemAPI.GetSingleton<ConstructSystemConfig>();
                _initEvent = true;
                ConstructWindow.Instance.EcsGhostShowTargetByTypeIndex += entity =>
                {
                    GhostShowTargetBuilding(entity);
                };
                ConstructWindow.Instance.EcsExitGhostShow += ExitGhostShow;
                BuildingDetailWindow.Instance.EcsGhostShowTarget += MovementGhostShowTargetBuilding;
                ConstructWindow.Instance.EcsEnterConstruct += () =>
                {
                    var data = SystemAPI.GetSingletonRW<ConstructCommandData>();
                    data.ValueRW.EnterConstruct = true;
                };
                ConstructWindow.Instance.EcsExitConstruct += () =>
                {
                    var data = SystemAPI.GetSingletonRW<ConstructCommandData>();
                    data.ValueRW.EnterConstruct = false;
                };
            }
        }

        protected override void OnUpdate()
        {
            // Update data
            var selectData = SystemAPI.GetSingleton<UnitSelectionData>();
            _inputData = SystemAPI.GetSingleton<InputConstructData>();
            _playerCurrentFaction = selectData.CurrentSelectFaction;
            // Check should enter construct mode by button
            CheckEnterByButton();
            // Not in construct mode, do nothing
            if (!ConstructWindow.Instance.IsOpened()) return;

            // Check if exit construct mode or exit ghost show
            if (CheckExit()) return;
            if (CheckCancel()) return;
            ref var commandData = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;
            if (commandData.CommandType == ConstructCommandType.None) return;
            // Check construct input
            CheckBuild(ref commandData);
            CheckRotate(ref commandData);
        }


        #region CheckInputMethods

        private void CheckEnterByButton()
        {
            // Check whether already enter or no enter command
            if (!_inputData.Enter) return;
            if (ConstructWindow.Instance.IsOpened()) return;
            ConstructWindow.Instance.Show();
            
        }

        private bool CheckExit()
        {
            if (!_inputData.Exit) return false;
            ExitGhostShow();
            if (ConstructWindow.Instance.IsOpened())
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

        private void CheckBuild(ref ConstructCommandData data)
        {
            if (!_inputData.Build) return;
            switch (data.State)
            {
                case PlacementStateType.Valid:
                    data.CommandType = ConstructCommandType.Build;
                    if (data.IsMovementShow)
                        _inGhostShow = false; // If this is movement show mode, exit after build target
                    var hitPosition = SystemAPI.GetSingleton<InputMouseData>().HitPosition;
                    AudioUtils.PlayAudioClip(AudioName.Construct, hitPosition,EntityManager);
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

        private void CheckRotate(ref ConstructCommandData data)
        {
            float angle;
            // if (_inputData.FineAdjustment)
            // {
            //     if (_inputData.LeftRotate)
            //         angle = -15;
            //     else if (_inputData.RightRotate)
            //         angle = 15;
            //     else
            //         angle = 0;
            //     data.RotationAngle = angle;
            //     return;
            // }

            if (math.abs(_inputData.Rotate) < 0.1f)
            {
                data.RotationAngle = 0f;
                return;
            }
            if (_inputData.LeftRotate )
                angle = -90;
            else if (_inputData.RightRotate)
                angle = 90;
            else
                angle = 0;
            data.RotationAngle = angle;

            /*angle = _inputData.Rotate * _config.RotateSpeed;
            data.RotationAngle = angle;*/
        }



        #endregion


        #region GhostShow

        private void GhostShowTargetBuilding(Entity target,  bool movementShow = false,
            LocalTransform oriTransform = default)
        {
            // First time enter building mode, need to create command data
            ref var data = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;
            if (!_inGhostShow)
            {
                data.TargetBuilding = target;
                data.CommandType = ConstructCommandType.Start;
                data.Faction = _playerCurrentFaction;
                data.GhostModelEntity = Entity.Null;
                data.GhostTriggerEntity = Entity.Null;
                data.RotationAngle = 0;
                data.State = PlacementStateType.Valid;
                data.IsMovementShow = movementShow;
                data.OriTransform = oriTransform;
                
                // data = new ConstructCommandData
                // {
                //     TargetBuilding = target,
                //     CommandType = ConstructCommandType.Start,
                //     Faction = _playerCurrentFaction,
                //     GhostModelEntity = Entity.Null,
                //     GhostTriggerEntity = Entity.Null,
                //     
                //     RotationAngle = 0,
                //     State = PlacementStateType.Valid,
                //     IsMovementShow = movementShow,
                //     OriTransform = oriTransform,
                //     EnterConstruct = true,
                //     PreviewCube = Entity.Null,
                //     PreviewAttackRangeEntity = Entity.Null,
                // };
                _inGhostShow = true;
                return;
            }
            data.TargetBuilding = target;
            data.CommandType = ConstructCommandType.Start;
        }

        private void MovementGhostShowTargetBuilding(Entity entity)
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
            ref var data = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;
            if(data.CommandType == ConstructCommandType.None)return;
            data.CommandType = ConstructCommandType.End;
            _inGhostShow = false;
        }

        #endregion


    }
}