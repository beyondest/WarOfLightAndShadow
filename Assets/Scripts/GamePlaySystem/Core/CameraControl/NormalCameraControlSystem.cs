using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.CustomInput.GamePlaySystem.Core.CustomInput.InputDataSystems;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.GamePlaySystem.CameraControl
{
    [UpdateAfter(typeof(InputCameraNormalModeSystem))]
    public partial class NormalCameraControlSystem : SystemBase
    {
        // Internal Data

        private Transform _cameraTransform;
        private Transform _rigTransform;
        private float3 _targetPosition;
        private float _zoomHeight;
        private float3 _horizontalVelocity;
        private float3 _lastPosition;
        private float3 _startDrag;
        private bool _preFlyMode;


        // Cache
        private NormalCameraControlConfig _config;
        private InputCameraNormalData _inputData;

        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<NormalCameraControlConfig>();
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<InputCameraNormalData>();
        }

        protected override void OnUpdate()
        {
            if (Camera.main == null) return;
            var cam = Camera.main;
            _config = SystemAPI.GetSingleton<NormalCameraControlConfig>();
            _inputData = SystemAPI.GetSingleton<InputCameraNormalData>();
            if (!_inputData.Enabled)
            {
                _preFlyMode = true;
                return;
            }
            InitCameraData(cam, _preFlyMode);
            _preFlyMode = false;
            
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            ref var cameraMovementState = ref SystemAPI.GetSingletonRW<CameraMovementState>().ValueRW;
            GetKeyboardMovement();
            RotateCamera();
            ZoomCamera(ref cameraMovementState);
            DragMoveCamera(in inputMouseData, ref cameraMovementState);
            if (_config.EdgeMoveEnabled)
                EdgeScrolling(ref cameraMovementState);
            UpdateRigTranslationVelocity();
            UpdateRigPosition();
            UpdateCameraZoomPosition();
        }

        private void InitCameraData(Camera cam, bool preFlyMode)
        {
            if (preFlyMode)
            {
                _rigTransform = cam.transform.GetChild(0);
                _rigTransform.SetParent(null);
                cam.transform.SetParent(_rigTransform);
                var rigNewPos = _rigTransform.position;
                rigNewPos.y = 0f;
                _rigTransform.position = rigNewPos;
                _rigTransform.rotation = quaternion.identity;
                _zoomHeight = 0.5f * (_config.MinHeight + _config.MaxHeight);
            }
            else
            {
                _rigTransform = cam.transform.parent;
                _cameraTransform = cam.transform;
                _zoomHeight = _cameraTransform.localPosition.y;
            }
            _cameraTransform.LookAt(_rigTransform);
            _lastPosition = _rigTransform.position;
        }


        #region CameraControl Methods

        private void GetKeyboardMovement()
        {
            var inputValue = _inputData.Movement.x * GetCameraRight()
                             + _inputData.Movement.y * GetCameraForward();
            if (!(math.length(inputValue) > 0.1f)) return;
            inputValue = math.normalize(inputValue);
            _targetPosition += inputValue;
        }

        private void RotateCamera()
        {
            var inputValue = _inputData.RotateCamera;
            if (math.abs(inputValue) < 0.1f) return;

            var speed = _inputData.SpeedUp
                ? _config.MaxRotationSpeed * _config.SpeedUpFactor
                : _config.MaxRotationSpeed;

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
                _targetPosition += _startDrag - inputMouseData.HitPosition;
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
                _targetPosition += GetCameraRight() * speed;
                cameraMovementState.EState = EdgeMoveState.Right;
            }

            // Move Left
            else if (Mouse.current.position.x.ReadValue() < _config.EdgeTolerance * Screen.width)
            {
                _targetPosition += GetCameraRight() * -speed;
                cameraMovementState.EState = EdgeMoveState.Left;
            }

            // Move Up
            if (Mouse.current.position.y.ReadValue() > Screen.height * (1 - _config.EdgeTolerance))
            {
                _targetPosition += GetCameraForward() * speed;
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
                _targetPosition += GetCameraForward() * -speed;
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
            _horizontalVelocity = ((float3)_rigTransform.transform.position - _lastPosition) / SystemAPI.Time.DeltaTime;
            _horizontalVelocity.y = 0f;
            _lastPosition = _rigTransform.transform.position;
        }

        private void UpdateRigPosition()
        {
            if (math.length(_targetPosition) > 0.1f)
            {
                //create a ramp up or acceleration
                var speed = math.lerp(_config.TranslationSpeed, _config.TranslationMaxSpeed,
                    SystemAPI.Time.DeltaTime * _config.TranslationAcceleration);
                if (_inputData.SpeedUp) speed *= _config.SpeedUpFactor;
                _rigTransform.position += (Vector3)_targetPosition * speed * SystemAPI.Time.DeltaTime;
            }
            else
            {
                //create smooth slow down
                _horizontalVelocity = math.lerp(_horizontalVelocity, float3.zero,
                    SystemAPI.Time.DeltaTime * _config.TranslationDamping);
                _rigTransform.position += (Vector3)_horizontalVelocity * SystemAPI.Time.DeltaTime;
            }

            //reset for next frame
            _targetPosition = float3.zero;
        }


        private void UpdateCameraZoomPosition()
        {
            //set zoom target
            var zoomTarget =
                new float3(_cameraTransform.localPosition.x, _zoomHeight, _cameraTransform.localPosition.z);
            //add float for forward/backward zoom
            zoomTarget -= _config.ZoomSpeed * (_zoomHeight - _cameraTransform.localPosition.y) * math.forward();
            _cameraTransform.localPosition =
                math.lerp(_cameraTransform.localPosition, zoomTarget, SystemAPI.Time.DeltaTime * _config.ZoomDamping);
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