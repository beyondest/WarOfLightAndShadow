using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public partial class MiniMapTransfer : SystemBase
    {
        private bool _initialized = false;
        private RectTransform _miniMapTransform;
        private RectTransform _squareTransform;
        private float _camMin;
        private float _camMax;
        private MapInfo _mapInfo;
        private bool _isDragging = false;

        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<MapInfo>();
            RequireForUpdate<MapInitInfo>();
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
                _camMin = -SystemAPI.GetSingleton<MapInitInfo>().tileSize;
                _camMax = _camMin + SystemAPI.GetSingleton<MapInfo>().OuterSquareSize;
                _mapInfo = SystemAPI.GetSingleton<MapInfo>();
            }
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
            local /= _mapInfo.OuterSquareSize;
            local = math.saturate(local);
            var squareTargetPos = new float2(local.x * _miniMapTransform.rect.width,
                local.z * _miniMapTransform.rect.height);
            _squareTransform.anchoredPosition = squareTargetPos;
            _squareTransform.rotation = Quaternion.Euler(0f,0f,-data.Angle);
            ClampSquarePos();
            
        }

        private void MiniMapMoveCamera()
        {
            _isDragging = true;
            var data = SystemAPI.GetSingletonRW<MiniMapControlData>();
            var local = new float2(_squareTransform.anchoredPosition.x / _miniMapTransform.rect.width,
                _squareTransform.anchoredPosition.y / _miniMapTransform.rect.height);
            data.ValueRW.MiniMapRequestPos = new float3
            (_camMin + local.x * _mapInfo.OuterSquareSize,
                0f, _camMin + local.y * _mapInfo.OuterSquareSize);
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