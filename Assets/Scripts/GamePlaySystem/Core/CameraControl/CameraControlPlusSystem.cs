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
    public partial class CameraControlPlusSystem : SystemBase
    {
        // Internal Data

        private float _speed;
        private Transform _cameraTransform;
        private Transform _rigTransform;
        private float3 _targetPosition;
        private float _zoomHeight;
        private float3 _horizontalVelocity;
        private float3 _lastPosition;
        private float3 _startDrag;

        // Cache
        private CameraControlConfig _config;
        private InputCameraControlData _inputData;


        protected override void OnStartRunning()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<InputCameraControlData>();
            _config = SystemAPI.GetSingleton<CameraControlConfig>();
        }

        protected override void OnUpdate()
        {
            if(Camera.main == null)return;
            var cam = Camera.main;
            InitCameraData(cam);
            _inputData = SystemAPI.GetSingleton<InputCameraControlData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            ref var cameraMovementState = ref SystemAPI.GetSingletonRW<CameraMovementState>().ValueRW;
            GetKeyboardMovement();
            RotateCamera();
            ZoomCamera(ref cameraMovementState);
            DragMoveCamera(in inputMouseData,ref cameraMovementState);
            if (_config.EdgeMoveEnabled)
                EdgeScrolling(ref cameraMovementState);
            UpdateVelocity();
            UpdateBasePosition();
            UpdateCameraPosition();
        }
        
        private void InitCameraData(Camera cam)
        {
            _rigTransform =cam.transform.parent;
            _cameraTransform =cam.transform;
            _zoomHeight = _cameraTransform.localPosition.y;
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
            var inputValue = _inputData.RotateCamera.x;
            if (!(math.abs(inputValue) > 0.1f)) return;
            
            var targetY = _rigTransform.rotation.eulerAngles.y + inputValue * _config.MaxRotationSpeed;
            var smoothedY = math.lerp(_rigTransform.rotation.eulerAngles.y, targetY, _config.RotationSmoothness * SystemAPI.Time.DeltaTime);
    
            _rigTransform.rotation = quaternion.Euler(0f, smoothedY, 0f);
            // _rigTransform.rotation =
                //     quaternion.Euler(0f, inputValue * _config.MaxRotationSpeed + _rigTransform.rotation.eulerAngles.y,
                //         _config.RotationSmoothness * SystemAPI.Time.DeltaTime);
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
            _zoomHeight = _cameraTransform.localPosition.y + inputValue * _config.StepSize;

            if (_zoomHeight < _config.MinHeight)
                _zoomHeight = _config.MinHeight;
            else if (_zoomHeight > _config.MaxHeight)
                _zoomHeight = _config.MaxHeight;
        }

        private void DragMoveCamera(
            in InputMouseData inputMouseData, ref CameraMovementState cameraMovementState)
        {
            if (_inputData.DragCameraStart)
            {
                _startDrag = inputMouseData.HitPosition;
                return;
            }
            if (_inputData.DraggingCamera)
            {
                cameraMovementState.IsDragging = true;
                _rigTransform.position += (Vector3)_startDrag - (Vector3)inputMouseData.HitPosition;
            }
            else
            {
                cameraMovementState.IsDragging = false;
            }
        }

        private void EdgeScrolling(ref CameraMovementState cameraMovementState)
        {
            var speed = _inputData.SpeedUp
                ? _config.EdgeMovementBaseSpeed * _config.SpeedUpFactor
                : _config.EdgeMovementBaseSpeed;
            cameraMovementState.EState = EdgeMoveState.Nothing;

            // Move Right
            if (Mouse.current.position.x.ReadValue() > Screen.width * (1 - _config.EdgeTolerance))
            {
                _rigTransform.position += (Vector3)GetCameraRight() * speed;
                cameraMovementState.EState = EdgeMoveState.Right;
            }

            // Move Left
            else if (Mouse.current.position.x.ReadValue() < _config.EdgeTolerance * Screen.width)
            {
                _rigTransform.position += (Vector3)GetCameraRight() * -speed;
                cameraMovementState.EState = EdgeMoveState.Left;
            }

            // Move Up
            if (Mouse.current.position.y.ReadValue() > Screen.height * (1 - _config.EdgeTolerance))
            {
                _rigTransform.position += (Vector3)GetCameraForward() * speed;
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
                _rigTransform.position += (Vector3)GetCameraForward() * -speed;
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

        
        private void UpdateVelocity()
        {
            _horizontalVelocity = ((float3)_rigTransform.transform.position - _lastPosition) / SystemAPI.Time.DeltaTime;
            _horizontalVelocity.y = 0f;
            _lastPosition = _rigTransform.transform.position;
        }
        private void UpdateBasePosition()
        {
            if (math.length(_targetPosition) > 0.1f)
            {
                var maxSpeed = _inputData.SpeedUp ? _config.MaxSpeed * _config.SpeedUpFactor : _config.MaxSpeed;
                //create a ramp up or acceleration
                _speed = math.lerp(_speed, maxSpeed, SystemAPI.Time.DeltaTime * _config.Acceleration);
                _rigTransform.position += (Vector3)_targetPosition * _speed * SystemAPI.Time.DeltaTime;
            }
            else
            {
                //create smooth slow down
                _horizontalVelocity = math.lerp(_horizontalVelocity, float3.zero,
                    SystemAPI.Time.DeltaTime * _config.Damping);
                _rigTransform.position += (Vector3)_horizontalVelocity * SystemAPI.Time.DeltaTime;
            }
            //reset for next frame
            _targetPosition = float3.zero;
        }
        
        
        private void UpdateCameraPosition()
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