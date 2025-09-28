using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.BasicControl.GlobalMonos;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable AsyncVoidMethod

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.Systems.General.Camera
{
    public partial class NormalCameraControlSystem : SystemBase
    {
        // Internal Dynamic Data

        private Transform _cameraTransform;
        private Transform _rigTransform;

        private float3 _targetRigPosDelta;

        // Each time you set this value to _cameraTransform.localPosition.y, will force the camera look at the rig at this height, at this view point
        private float _zoomHeight;
        private float3 _horizontalVelocity;
        private float3 _lastPosition;

        private float3 _startDrag;

        // private bool _preFlyMode;
        private UnityEngine.Camera _camera;


        // Cache
        private NormalCameraControlConfig _config;
        private InputCameraNormalData _inputData;
        private bool _initialized;
        private bool _isRoaming;

        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<NormalCameraControlConfig>();
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<InputCameraNormalData>();
            RequireForUpdate<WaitInfo>();
            EntityManager.CreateSingleton(new CameraMainGameplayHistory());
        }


        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                GameController.Instance.OnSwitchGameStatus += SetCameraPositionWhenSwitchSubGameplay;
                RoamingCameraController.Instance.OnStartRoamingCamera += () => _isRoaming = true;
                RoamingCameraController.Instance.OnEndRoamingCamera += () => _isRoaming = false;
            }
        }

        protected override void OnUpdate()
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                _config = SystemAPI.GetComponent<NormalCameraControlConfig>(
                    SystemAPI.GetSingletonEntity<MainGameCameraTag>());
                // _camera = CameraController.Instance.mainGameCamera;
                GetSetCamera();
                _zoomHeight = _cameraTransform.localPosition.y;
                var saveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;
                var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
                if (saveSlot == SaveUtilities.NewGameSaveSlot)
                {
                    var startPos = SystemAPI.GetSingleton<CameraStartPosData>();
                    _rigTransform.position = playerFaction == FactionTag.Light
                        ? startPos.LightInitStartPos
                        : startPos.DarkInitStartPos;
                    SystemAPI.SetSingleton(new CameraMainGameplayHistory
                    {
                        localPosition = _cameraTransform.localPosition,
                        localRotation = _cameraTransform.localRotation,
                        rigPosition = _rigTransform.position,
                        rigRotation = _rigTransform.rotation,
                    });
                }
                else
                {
                    var history = SystemAPI.GetSingleton<CameraMainGameplayHistory>();
                    _cameraTransform.localPosition = history.localPosition;
                    _cameraTransform.localRotation = history.localRotation;
                    _rigTransform.position = history.rigPosition;
                    _rigTransform.rotation = history.rigRotation;
                    _zoomHeight = _cameraTransform.localPosition.y;
                }

                return;
            }

            if (gameStatus != GameStatus.SubGaming && gameStatus != GameStatus.MainGaming)
                return;

            // If player is waiting, do not control camera
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if (waitInfo.WaitType != WaitType.None) return;


            GetSetCamera();
            LookAt();

            CameraData cameraData;
            if (_isRoaming)
            {
                // Update camera data
                cameraData = SystemAPI.GetSingleton<CameraData>();
                cameraData.CameraRigPosition = _rigTransform.position;
                LookAt();
                CheckAndAssignMiniMapCam();
                SystemAPI.SetSingleton(cameraData);
                return;
            }

            _config = SystemAPI.GetComponent<NormalCameraControlConfig>(gameStatus == GameStatus.MainGaming
                ? SystemAPI.GetSingletonEntity<MainGameCameraTag>()
                :
                // _camera = CameraController.Instance.mainGameCamera;
                SystemAPI.GetSingletonEntity<SubGameCameraTag>());

            // _camera = CameraController.Instance.subGameCamera;
            _inputData = SystemAPI.GetSingleton<InputCameraNormalData>();
            // var flyModeData = SystemAPI.GetSingleton<InputCameraFlyData>();
            // if (flyModeData.Enabled)
            // {
            //     _preFlyMode = true;
            //     return;
            // }

            // _preFlyMode = false;

            // Control camera
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            ref var cameraMovementState = ref SystemAPI.GetSingletonRW<CameraMovementState>().ValueRW;
            GetKeyboardMovement();
            RotateCamera();
            ZoomCamera(ref cameraMovementState);
            DragMoveCamera(in inputMouseData, ref cameraMovementState);
            if (_config.edgeMoveEnabled)
                EdgeScrolling(ref cameraMovementState);
            UpdateRigTranslationVelocity();
            UpdateRigPosition();
            UpdateCameraZoomPosition();
            // LimitCamera();


            if (gameStatus == GameStatus.SubGaming)
            {
                CheckAndAssignMiniMapCam();
            }

            if (gameStatus == GameStatus.MainGaming)
            {
                SystemAPI.SetSingleton(new CameraMainGameplayHistory
                {
                    localPosition = _cameraTransform.localPosition,
                    localRotation = _cameraTransform.localRotation,
                    rigPosition = _rigTransform.position,
                    rigRotation = _rigTransform.rotation,
                });
            }

            // Update camera dataw
            cameraData = SystemAPI.GetSingleton<CameraData>();
            cameraData.CameraRigPosition = _rigTransform.position;
            SystemAPI.SetSingleton(cameraData);
        }

        private void GetSetCamera()
        {
            _camera = UnityEngine.Camera.main;
            _rigTransform = _camera!.transform.parent;
            _cameraTransform = _camera.transform;
        }


        private void LookAt()
        {
            /*if (_preFlyMode)
            {
                _rigTransform = _camera.transform.GetChild(1);
                _rigTransform.SetParent(null);
                _camera.transform.SetParent(_rigTransform);
                var rigNewPos = _rigTransform.position;
                rigNewPos.y = 0f;
                _rigTransform.position = rigNewPos;
                _rigTransform.rotation = quaternion.identity;
                _zoomHeight = 0.5f * (_config.MinHeight + _config.MaxHeight);
            }*/
            // else


            _cameraTransform.LookAt(_rigTransform);
            _lastPosition = _rigTransform.position;
        }


        #region CameraControl Methods

        private void CheckAndAssignMiniMapCam()
        {
            var dataEntity = SystemAPI.GetSingletonEntity<MiniMapControlData>();
            var data = SystemAPI.GetSingletonRW<MiniMapControlData>();
            if (SystemAPI.IsComponentEnabled<DraggingTag>(dataEntity))
            {
                // _targetRigPosDelta = data.ValueRW.MiniMapRequestPos - (float3)_rigTransform.position;
                _rigTransform.position = data.ValueRW.MiniMapRequestPos;
                SystemAPI.SetComponentEnabled<DraggingTag>(dataEntity, false);
            }

            data.ValueRW.CameraRigWorldPos = _rigTransform.position;
            var forward = _rigTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            data.ValueRW.Angle = math.degrees(math.atan2(forward.x, forward.z));
        }

        private void GetKeyboardMovement()
        {
            var inputValue = _inputData.Movement.x * GetCameraRight()
                             + _inputData.Movement.y * GetCameraForward();
            if (!(math.length(inputValue) > 0.1f)) return;
            inputValue = math.normalize(inputValue);
            var scale = _inputData.SpeedUp ? _config.speedUpFactor : 1f;
            _targetRigPosDelta += inputValue * _config.speedForWasd * scale;
        }

        private void RotateCamera()
        {
            var inputValue = _inputData.RotateCamera;
            if (math.abs(inputValue) < 0.1f) return;

            var speed = _inputData.SpeedUp
                ? _config.maxRotationSpeed * _config.speedUpFactor
                : _config.maxRotationSpeed;

            quaternion deltaRotation = quaternion.RotateY(
                math.radians(inputValue * speed * SystemAPI.Time.DeltaTime)
            );

            _rigTransform.rotation = math.mul(
                _rigTransform.rotation,
                deltaRotation);
        }

        private void ZoomCamera(ref CameraMovementState cameraMovementState)
        {
            var inputValue = -_inputData.ZoomCamera.y;
            cameraMovementState.ZState = CameraZoomState.Nothing;
            if (!(math.abs(inputValue) > 0.1f)) return;
            cameraMovementState.ZState = inputValue switch
            {
                > 0 => CameraZoomState.ZoomIn,
                < 0 => CameraZoomState.ZoomOut,
                _ => cameraMovementState.ZState
            };
            var stepSize = _inputData.SpeedUp
                ? _config.zoomHeightStepSize * _config.speedUpFactor
                : _config.zoomHeightStepSize;
            _zoomHeight = _cameraTransform.localPosition.y + inputValue * stepSize;
            if (_zoomHeight < _config.minHeight)
                _zoomHeight = _config.minHeight;
            else if (_zoomHeight > _config.maxHeight)
                _zoomHeight = _config.maxHeight;
        }

        private void DragMoveCamera(
            in InputMouseData inputMouseData, ref CameraMovementState cameraMovementState)
        {
            if (_inputData.DragCameraStart || math.abs(_inputData.RotateCamera) > 0.1f)
            {
                _startDrag = inputMouseData.HitPosition;
                return;
            }

            if (_inputData.DraggingCamera)
            {
                cameraMovementState.IsDragging = true;
                _targetRigPosDelta += _startDrag - inputMouseData.HitPosition;
            }
            else
            {
                cameraMovementState.IsDragging = false;
            }
        }

        private void EdgeScrolling(ref CameraMovementState cameraMovementState)
        {
            if (!_inputData.EdgeScrolling || math.abs(_inputData.RotateCamera) > 0.1f)
                return; // Only perform edge scrolling when mode enabled and not rotating

            var speed = _inputData.SpeedUp
                ? _config.edgeMovementBaseSpeed * _config.speedUpFactor
                : _config.edgeMovementBaseSpeed;
            cameraMovementState.EState = EdgeMoveState.Nothing;

            // Move Right
            if (Mouse.current.position.x.ReadValue() > Screen.width * (1 - _config.edgeTolerance))
            {
                _targetRigPosDelta += GetCameraRight() * speed;
                cameraMovementState.EState = EdgeMoveState.Right;
            }

            // Move Left
            else if (Mouse.current.position.x.ReadValue() < _config.edgeTolerance * Screen.width)
            {
                _targetRigPosDelta += GetCameraRight() * -speed;
                cameraMovementState.EState = EdgeMoveState.Left;
            }

            // Move Up
            if (Mouse.current.position.y.ReadValue() > Screen.height * (1 - _config.edgeTolerance))
            {
                _targetRigPosDelta += GetCameraForward() * speed;
                cameraMovementState.EState = cameraMovementState.EState switch
                {
                    EdgeMoveState.Right => EdgeMoveState.RightUp,
                    EdgeMoveState.Left => EdgeMoveState.LeftUp,
                    _ => EdgeMoveState.Up
                };
            }

            // Move Down
            else if (Mouse.current.position.y.ReadValue() < _config.edgeTolerance * Screen.height)
            {
                _targetRigPosDelta += GetCameraForward() * -speed;
                cameraMovementState.EState = cameraMovementState.EState switch
                {
                    EdgeMoveState.Right => EdgeMoveState.RightDown,
                    EdgeMoveState.Left => EdgeMoveState.LeftDown,
                    _ => EdgeMoveState.Down
                };
            }
        }

        #endregion


        #region Update CameraRig and Camera Methods

        private void UpdateRigTranslationVelocity()
        {
            _horizontalVelocity = ((float3)_rigTransform.transform.position - _lastPosition) /
                                  SystemAPI.Time.DeltaTime;
            _horizontalVelocity.y = 0f;
            _lastPosition = _rigTransform.transform.position;
        }

        private void UpdateRigPosition()
        {
            if (math.length(_targetRigPosDelta) > 0.1f)
            {
                //create a ramp up or acceleration
                var speed = math.lerp(_config.speedForTargetMoving, _config.translationMaxSpeed,
                    SystemAPI.Time.DeltaTime * _config.translationAcceleration);
                if (_inputData is { SpeedUp: true, DraggingCamera: false }) speed *= _config.speedUpFactor;
                _rigTransform.position +=
                    (Vector3)_targetRigPosDelta * speed * SystemAPI.Time.DeltaTime;
            }
            else
            {
                //create smooth slow down
                _horizontalVelocity = math.lerp(_horizontalVelocity, float3.zero,
                    SystemAPI.Time.DeltaTime * _config.translationDamping);
                if (!math.any(math.isnan(_horizontalVelocity)))
                {
                    _rigTransform.position +=
                        (Vector3)_horizontalVelocity * SystemAPI.Time.DeltaTime;
                }
                // _rigTransform.position += (Vector3)_horizontalVelocity * SystemAPI.Time.DeltaTime;
            }

            //reset for next frame
            _targetRigPosDelta = float3.zero;
        }


        private void UpdateCameraZoomPosition()
        {
            //set zoom target
            var zoomTarget =
                new float3(_cameraTransform.localPosition.x, _zoomHeight, _cameraTransform.localPosition.z);
            //add float for forward/backward zoom
            zoomTarget -= _config.zoomSpeed * (_zoomHeight - _cameraTransform.localPosition.y) * math.forward();

            _cameraTransform.localPosition =
                math.lerp(_cameraTransform.localPosition, zoomTarget,
                    SystemAPI.Time.DeltaTime * _config.zoomDamping);
            _cameraTransform.LookAt(_rigTransform.transform);
        }

        #endregion


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float3 GetCameraForward()
        {
            float3 forward = _cameraTransform.forward;
            forward.y = 0f;
            return forward;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float3 GetCameraRight()
        {
            float3 right = _cameraTransform.right;
            right.y = 0f;
            return right;
        }

        private void SetCameraPositionWhenSwitchSubGameplay(SubGameStatusData targetSubGameStatusData,
            SubGameStatusData currentSubGameStatusData)
        {
            GetSetCamera();

            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.PlayerCity)
            {
                if (currentSubGameStatusData.SubGameStatus != SubGameStatus.None)
                    return; // When player stay to city after war, camera should not set to new position
            }

            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.None)
            {
                var history = SystemAPI.GetSingleton<CameraMainGameplayHistory>();
                _cameraTransform.localPosition = history.localPosition;
                _cameraTransform.localRotation = history.localRotation;
                _rigTransform.position = history.rigPosition;
                _rigTransform.rotation = history.rigRotation;
                _zoomHeight = _cameraTransform.localPosition.y;
                return;
            }

            CheckAddCameraReferences(targetSubGameStatusData);

            RoamingCameraAmongPositions(SystemAPI.GetSingletonBuffer<CameraRoamingPosition>());
        }

        private void CheckAddCameraReferences(SubGameStatusData targetSubGameStatusData)
        {
            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.Encounter)
            {
                return;
            }

            if (targetSubGameStatusData.SubGameStatus is SubGameStatus.PlayerDefend or SubGameStatus.PlayerCity)
            {
                using var query =
                    EntityManager.CreateEntityQuery(typeof(PlayerTag), typeof(CrystalDef), typeof(LocalTransform));
                var pos = query.GetSingleton<LocalTransform>().Position;
                var buffer = SystemAPI.GetSingletonBuffer<CameraRoamingPosition>();
                buffer.Add(new CameraRoamingPosition
                {
                    Value = pos,
                    IsEnemy = targetSubGameStatusData.SubGameStatus == SubGameStatus.PlayerSiege
                });
            }
            else
            {
                using var query =
                    EntityManager.CreateEntityQuery(typeof(AITag), typeof(CrystalDef), typeof(LocalTransform));
                var pos = query.GetSingleton<LocalTransform>().Position;
                var buffer = SystemAPI.GetSingletonBuffer<CameraRoamingPosition>();
                buffer.Add(new CameraRoamingPosition
                {
                    Value = pos,
                    IsEnemy = targetSubGameStatusData.SubGameStatus == SubGameStatus.PlayerSiege
                });
            }
        }

        private void RoamingCameraAmongPositions(DynamicBuffer<CameraRoamingPosition> positions)
        {
            _cameraTransform.localPosition = SystemAPI.GetSingleton<CameraStartPosData>().SubGameplayCameraLocalPos;
            _zoomHeight = _cameraTransform.localPosition.y;
            if (positions.Length == 1) // No roaming at all, because this is player city
            {
                _rigTransform.position = positions[0].Value;
                return;
            }

            var enemyPositions = new List<float3>();
            var playerPositions = new List<float3>();
            foreach (var position in positions)
            {
                if (position.IsEnemy)
                {
                    enemyPositions.Add(position.Value);
                }
                else
                {
                    playerPositions.Add(position.Value);
                }
            }


            var totalPositions = new List<float3>();
            foreach (var position in enemyPositions)
            {
                totalPositions.Add(position);
            }

            foreach (var position in playerPositions)
            {
                totalPositions.Add(position);
            }


            RoamingCameraController.Instance.StartRoamingCamera(_rigTransform, totalPositions,
                enemyPositions.Count);
        }
    }
}