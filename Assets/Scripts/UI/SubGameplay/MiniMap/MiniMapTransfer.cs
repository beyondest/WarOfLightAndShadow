using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.Map;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public partial class MiniMapTransfer : SystemBase
    {
        private bool _initialized;
        private RectTransform _miniMapTransform;
        private RectTransform _squareTransform;
        private Camera _miniMapCam;
        private float _camMin;
        private float _camMax;
        private bool _isDragging;
        private float _length;

        protected override void OnCreate()
        {
            RequireForUpdate<CurrentSubMapInfo>();
            RequireForUpdate<SubGamingTag>();
            RequireForUpdate<MiniMapControlData>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                _initialized = true;
                MiniMapWindow.Instance.OnEcsOnSquareDrag += MiniMapMoveCamera;
                _miniMapTransform = MiniMapWindow.Instance.miniMapRect;
                _squareTransform = MiniMapWindow.Instance.miniMapSquareRect;
                _miniMapCam = MiniMapWindow.Instance.miniMapCamera;
            }


            var config = SystemAPI.GetSingleton<MiniMapConfig>();
            _camMin = SystemAPI.GetSingleton<CurrentSubMapInfo>().MapInfo.CameraMinCoordinate;
            _camMax = SystemAPI.GetSingleton<CurrentSubMapInfo>().MapInfo.CameraMaxCoordinate;
            _length = _camMax - _camMin;
            var centerValue = (_camMax + _camMin) / 2;
            var centerPos = new float3(centerValue, config.MiniMapCameraHeight, centerValue);
            _miniMapCam.transform.position = centerPos;
            _miniMapCam.orthographicSize = _length / 2;
        }

        protected override void OnUpdate()
        {
            if (_isDragging)
            {
                _isDragging = false;
                ClampSquarePos();
                return;
            }

            var data = SystemAPI.GetSingleton<MiniMapControlData>();
            var local = data.CameraRigWorldPos - new float3(_camMin, 0f, _camMin);
            local /= _length;
            local = math.saturate(local);
            var squareTargetPos = new float2(local.x * _miniMapTransform.rect.width,
                local.z * _miniMapTransform.rect.height);
            _squareTransform.anchoredPosition = squareTargetPos;
            _squareTransform.rotation = Quaternion.Euler(0f, 0f, -data.Angle);
            ClampSquarePos();
        }

        private void MiniMapMoveCamera()
        {
            _isDragging = true;
            var data = SystemAPI.GetSingletonRW<MiniMapControlData>();
            var local = new float2(_squareTransform.anchoredPosition.x / _miniMapTransform.rect.width,
                _squareTransform.anchoredPosition.y / _miniMapTransform.rect.height);
            data.ValueRW.MiniMapRequestPos = new float3
            (_camMin + local.x *_length,
                0f, _camMin + local.y *_length);
            var dataEntity = SystemAPI.GetSingletonEntity<MiniMapControlData>();
            SystemAPI.SetComponentEnabled<DraggingTag>(dataEntity, true);
        }

        private void ClampSquarePos()
        {
            var pos = _squareTransform.anchoredPosition;
            pos.x = math.clamp(pos.x, 0f, _miniMapTransform.rect.width);
            pos.y = math.clamp(pos.y, 0f, _miniMapTransform.rect.height);
            _squareTransform.anchoredPosition = pos;
        }
    }
}