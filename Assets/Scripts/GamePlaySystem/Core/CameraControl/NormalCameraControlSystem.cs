using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.GamePlaySystem.CameraControl
{
    public partial class NormalCameraControlSystem : SystemBase
    {
        // Internal Dynamic Data

        private Transform _cameraTransform;
        private Transform _rigTransform;
        private float3 _targetRigPosDelta;
        private float _zoomHeight;
        private float3 _horizontalVelocity;
        private float3 _lastPosition;

        private float3 _startDrag;

        // private bool _preFlyMode;
        private Camera _camera;

        // Limit
        private float _minPos;
        private float _maxPos;

        // Cache
        private NormalCameraControlConfig _config;
        private InputCameraNormalData _inputData;

        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<NormalCameraControlConfig>();
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<InputCameraNormalData>();
            RequireForUpdate<MapInfo>();
        }

        protected override void OnStartRunning()
        {
            _config = SystemAPI.GetSingleton<NormalCameraControlConfig>();
            var mapInitInfo = SystemAPI.GetSingleton<MapInitInfo>();
            var mapInfo = SystemAPI.GetSingleton<MapInfo>();
            _minPos = -mapInitInfo.tileSize / 2f - _config.LimitPosBias;
            _maxPos = -mapInitInfo.tileSize / 2f + mapInfo.OuterSquareSize + _config.LimitPosBias;
        }

        protected override void OnUpdate()
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                _camera = Camera.main;
                _rigTransform = _camera!.transform.parent;
                _cameraTransform = _camera.transform;
                _zoomHeight = _cameraTransform.localPosition.y;
                _rigTransform.position = SystemAPI.GetSingleton<PlayerFirstBasePos>().Value;
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerFirstBasePos>());
                return;
            }
            if (gameStatus != GameStatus.Gaming)
                return;
            _inputData = SystemAPI.GetSingleton<InputCameraNormalData>();
            // var flyModeData = SystemAPI.GetSingleton<InputCameraFlyData>();
            // if (flyModeData.Enabled)
            // {
            //     _preFlyMode = true;
            //     return;
            // }

            LookAt();
            // _preFlyMode = false;

            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            ref var cameraMovementState = ref SystemAPI.GetSingletonRW<CameraMovementState>().ValueRW;
            GetMiniMapSquarePos();
            GetKeyboardMovement();
            RotateCamera();
            ZoomCamera(ref cameraMovementState);
            DragMoveCamera(in inputMouseData, ref cameraMovementState);
            if (_config.EdgeMoveEnabled)
                EdgeScrolling(ref cameraMovementState);
            UpdateRigTranslationVelocity();
            UpdateRigPosition();
            UpdateCameraZoomPosition();
            // LimitCamera();
        }

        private void LimitCamera()
        {
            var position = _cameraTransform.position;
            position.x = math.clamp(position.x, _minPos, _maxPos);
            position.z = math.clamp(position.z, _minPos, _maxPos);
            _cameraTransform.position = position;
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

        private void GetMiniMapSquarePos()
        {
            var dataEntity = SystemAPI.GetSingletonEntity<MiniMapControlData>();
            var data = SystemAPI.GetSingletonRW<MiniMapControlData>();
            if (SystemAPI.IsComponentEnabled<DraggingTag>(dataEntity))
            {
                _targetRigPosDelta = data.ValueRW.MiniMapRequestPos - (float3)_rigTransform.position;
                // _rigTransform.position = data.ValueRW.MiniMapRequestPos;
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
            _targetRigPosDelta += inputValue;
        }

        private void RotateCamera()
        {
            var inputValue = _inputData.RotateCamera;
            if (math.abs(inputValue) < 0.1f) return;

            var speed = _inputData.SpeedUp
                ? _config.MaxRotationSpeed * _config.SpeedUpFactor
                : _config.MaxRotationSpeed;

            quaternion deltaRotation = quaternion.RotateY(
                math.radians(inputValue * speed * SystemAPI.GetSingleton<GameTimeData>().DeltaTime)
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
                ? _config.ZoomHeightStepSize * _config.SpeedUpFactor
                : _config.ZoomHeightStepSize;
            _zoomHeight = _cameraTransform.localPosition.y + inputValue * stepSize;
            if (_zoomHeight < _config.MinHeight)
                _zoomHeight = _config.MinHeight;
            else if (_zoomHeight > _config.MaxHeight)
                _zoomHeight = _config.MaxHeight;
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
                ? _config.EdgeMovementBaseSpeed * _config.SpeedUpFactor
                : _config.EdgeMovementBaseSpeed;
            cameraMovementState.EState = EdgeMoveState.Nothing;

            // Move Right
            if (Mouse.current.position.x.ReadValue() > Screen.width * (1 - _config.EdgeTolerance))
            {
                _targetRigPosDelta += GetCameraRight() * speed;
                cameraMovementState.EState = EdgeMoveState.Right;
            }

            // Move Left
            else if (Mouse.current.position.x.ReadValue() < _config.EdgeTolerance * Screen.width)
            {
                _targetRigPosDelta += GetCameraRight() * -speed;
                cameraMovementState.EState = EdgeMoveState.Left;
            }

            // Move Up
            if (Mouse.current.position.y.ReadValue() > Screen.height * (1 - _config.EdgeTolerance))
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
            else if (Mouse.current.position.y.ReadValue() < _config.EdgeTolerance * Screen.height)
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
                                  SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
            _horizontalVelocity.y = 0f;
            _lastPosition = _rigTransform.transform.position;
        }

        private void UpdateRigPosition()
        {
            if (math.length(_targetRigPosDelta) > 0.1f)
            {
                //create a ramp up or acceleration
                var speed = math.lerp(_config.TranslationSpeed, _config.TranslationMaxSpeed,
                    SystemAPI.GetSingleton<GameTimeData>().DeltaTime * _config.TranslationAcceleration);
                if (_inputData.SpeedUp) speed *= _config.SpeedUpFactor;
                _rigTransform.position +=
                    (Vector3)_targetRigPosDelta * speed * SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
            }
            else
            {
                //create smooth slow down
                _horizontalVelocity = math.lerp(_horizontalVelocity, float3.zero,
                    SystemAPI.GetSingleton<GameTimeData>().DeltaTime * _config.TranslationDamping);
                if (!math.any(math.isnan(_horizontalVelocity)))
                {
                    _rigTransform.position +=
                        (Vector3)_horizontalVelocity * SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
                }
                // _rigTransform.position += (Vector3)_horizontalVelocity * SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
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
            zoomTarget -= _config.ZoomSpeed * (_zoomHeight - _cameraTransform.localPosition.y) * math.forward();
            _cameraTransform.localPosition =
                math.lerp(_cameraTransform.localPosition, zoomTarget,
                    SystemAPI.GetSingleton<GameTimeData>().DeltaTime * _config.ZoomDamping);
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
    }
}