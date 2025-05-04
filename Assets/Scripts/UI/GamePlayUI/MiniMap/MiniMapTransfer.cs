using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.UI.GamePlay.UI.GamePlayUI.MiniMap
{
    public partial class MiniMapTransfer : SystemBase
    {
        private bool _initialized = false;
        private bool _isDragging = false;
        private RectTransform _miniMapTransform;
        private RectTransform _squareTransform;
        private float _camMin;
        private float _camMax;
        private MapInfo _mapInfo;

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
                MiniMapWindow.Instance.EcsOnSquareDrag += MiniMapMoveCamera;
                _miniMapTransform = MiniMapWindow.Instance.miniMapRect;
                _squareTransform = MiniMapWindow.Instance.miniMapSquareRect;
                _camMin = -SystemAPI.GetSingleton<MapInitInfo>().tileSize;
                _camMax = _camMin + SystemAPI.GetSingleton<MapInfo>().OuterSquareSize;
                _mapInfo = SystemAPI.GetSingleton<MapInfo>();
            }
        }

        protected override void OnUpdate()
        {
            var data = SystemAPI.GetSingletonRW<MiniMapControlData>();
            data.ValueRW.IsDragging = _isDragging;

            if (!_isDragging)
            {
                var local = data.ValueRO.RigPos - new float3(_camMin, 0f, _camMin);
                local = math.clamp(local, float3.zero, new float3(_camMax, 0f, _camMax));
                local /= _mapInfo.OuterSquareSize;
                var squareTargetPos = new float2(local.x * _miniMapTransform.rect.width,
                    local.z * _miniMapTransform.rect.height);
                _squareTransform.anchoredPosition = squareTargetPos;
            }
            else
            {
                var local = new float2(_squareTransform.anchoredPosition.x / _miniMapTransform.rect.width,
                    _squareTransform.anchoredPosition.y / _miniMapTransform.rect.height);
                data.ValueRW.TargetPos = new float3
                    (_camMin + local.x * _mapInfo.OuterSquareSize,
                        0f, _camMin + local.y * _mapInfo.OuterSquareSize);
            }
            _isDragging = false;
        }

        private void MiniMapMoveCamera()
        {
            _isDragging = true;
        }
    }
}