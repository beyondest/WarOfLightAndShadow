using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
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


        // Cache
        private NormalCameraControlConfig _config;
        private InputCameraNormalData _inputData;

        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<NormalCameraControlConfig>();
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<InputCameraNormalData>();
            RequireForUpdate<PlayerFactionData>();
        }


        protected override void OnUpdate()
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                _config = SystemAPI.GetComponent<NormalCameraControlConfig>(
                    SystemAPI.GetSingletonEntity<MainGameCameraTag>());

                _camera = CameraController.Instance.mainGameCamera;
                _rigTransform = _camera!.transform.parent;
                _cameraTransform = _camera.transform;
                _zoomHeight = _cameraTransform.localPosition.y;
                var startPos = SystemAPI.GetSingleton<CameraStartPos>();
                _rigTransform.position = SystemAPI.GetSingleton<PlayerFactionData>().Value == FactionTag.Ally
                    ? startPos.Light
                    : startPos.Dark;
                return;
            }

            if (gameStatus != GameStatus.SubGaming && gameStatus != GameStatus.MainGaming)
                return;
            if (gameStatus == GameStatus.MainGaming)
            {
                _config = SystemAPI.GetComponent<NormalCameraControlConfig>(
                    SystemAPI.GetSingletonEntity<MainGameCameraTag>());
                _camera = CameraController.Instance.mainGameCamera;
            }
            else
            {
                _config = SystemAPI.GetComponent<NormalCameraControlConfig>(
                    SystemAPI.GetSingletonEntity<SubGameCameraTag>());
                _camera = CameraController.Instance.subGameCamera;
            }

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
            if (_config.edgeMoveEnabled)
                EdgeScrolling(ref cameraMovementState);
            UpdateRigTranslationVelocity();
            UpdateRigPosition();
            UpdateCameraZoomPosition();
            // LimitCamera();
            var cameraData = SystemAPI.GetSingleton<CameraData>();
            cameraData.CameraRigPosition = _rigTransform.position;
            SystemAPI.SetSingleton(cameraData);
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
                                  SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
            _horizontalVelocity.y = 0f;
            _lastPosition = _rigTransform.transform.position;
        }

        private void UpdateRigPosition()
        {
            if (math.length(_targetRigPosDelta) > 0.1f)
            {
                //create a ramp up or acceleration
                var speed = math.lerp(_config.speedForTargetMoving, _config.translationMaxSpeed,
                    SystemAPI.GetSingleton<GameTimeData>().DeltaTime * _config.translationAcceleration);
                if (_inputData is { SpeedUp: true, DraggingCamera: false }) speed *= _config.speedUpFactor;
                _rigTransform.position +=
                    (Vector3)_targetRigPosDelta * speed * SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
            }
            else
            {
                //create smooth slow down
                _horizontalVelocity = math.lerp(_horizontalVelocity, float3.zero,
                    SystemAPI.GetSingleton<GameTimeData>().DeltaTime * _config.translationDamping);
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
            zoomTarget -= _config.zoomSpeed * (_zoomHeight - _cameraTransform.localPosition.y) * math.forward();
            
            _cameraTransform.localPosition =
                math.lerp(_cameraTransform.localPosition, zoomTarget,
                    SystemAPI.GetSingleton<GameTimeData>().DeltaTime * _config.zoomDamping);
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