using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public partial class FlyCameraControlSystem : SystemBase
    {
        
        private float _yaw;
        private float _pitch;
        private Transform _camTransform;
        private Transform _rigTransform;
        private Camera _camera;
        private bool _preNormalMode;
        
        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<InputCameraFlyData>();
            RequireForUpdate<FlyCameraControlConfig>();
        }

        protected override void OnStartRunning()
        {
            _camera = Camera.main;
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
            var inputData = SystemAPI.GetSingleton<InputCameraFlyData>();
            var inputNormalData = SystemAPI.GetSingleton<InputCameraNormalData>();
            var config = SystemAPI.GetSingleton<FlyCameraControlConfig>();
            if (inputNormalData.Enabled)
            {
                _preNormalMode = true;
                return;
            }
            if (_preNormalMode)
            {
                // Switch parent
                _rigTransform = _camera.transform.parent;
                _camera.transform.SetParent(null);
                _rigTransform.SetParent(_camera.transform);
                _preNormalMode = false;
            }
            
            _camTransform = _camera.transform;
            
            // Look
            _yaw += inputData.LookDelta.x * config.LookSpeedH *deltaTime;
            _pitch -= inputData.LookDelta.y * config.LookSpeedV * deltaTime;
            _pitch = math.clamp(_pitch, -89f, 89f);
            _camTransform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            
            // Move
            var move = new Vector3(inputData.Move.x, 0, inputData.Move.y);
            if (inputData.FlyUp) move.y += 1;
            if (inputData.FlyDown) move.y -= 1;
            var speed = config.FlySpeed * (inputData.SpeedUp ? config.SpeedUpMultiplier  : 1f);
            _camTransform.Translate(move * speed * deltaTime, Space.Self);

            // Zoom
            _camTransform.Translate(Vector3.forward * inputData.Zoom.y * config.ZoomSpeed * deltaTime, Space.Self);
        }
    }
}