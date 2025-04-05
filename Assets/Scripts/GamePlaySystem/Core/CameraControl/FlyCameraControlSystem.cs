using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.CustomInput.GamePlaySystem.Core.CustomInput.InputDataSystems;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    [UpdateAfter(typeof(InputCameraFlyModeSystem))]
    public partial class FlyCameraControlSystem : SystemBase
    {
        
        private float _yaw;
        private float _pitch;
        private Transform _camTransform;
        private Transform _rigTransform;
        private bool _preNormalMode;
        
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputCameraFlyData>();
            RequireForUpdate<FlyCameraControlConfig>();
        }

       

        
    
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if(Camera.main == null) return;
            var cam = Camera.main;
            var inputData = SystemAPI.GetSingleton<InputCameraFlyData>();
            var config = SystemAPI.GetSingleton<FlyCameraControlConfig>();
            if (!inputData.Enabled)
            {
                _preNormalMode = true;
                return;
            }
            if (_preNormalMode)
            {
                // Switch parent
                _rigTransform = cam.transform.parent;
                cam.transform.SetParent(null);
                _rigTransform.SetParent(cam.transform);
                _preNormalMode = false;
            }
            
            _camTransform = cam.transform;
            
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