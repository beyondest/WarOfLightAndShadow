using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Physics;
using SparFlame.GamePlaySystem.General;
using UnityEngine.EventSystems;

namespace SparFlame.GamePlaySystem.CustomInput
{
    [UpdateAfter(typeof(GameBasicControlSystem))]
    public partial class InputMouseSystem : SystemBase
    {
        private Camera _camera;
        private float _currentClickInterval;
        private bool _isDoubleClick;

        private CustomMouseSystemConfig _config;
        private CustomMouseKeyMapping _mouseKeyMapping;

        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputMouseData>();
            
        }

        protected override void OnStartRunning()
        {
            _camera = Camera.main;
            _config = SystemAPI.GetSingleton<CustomMouseSystemConfig>();
            _mouseKeyMapping = SystemAPI.GetSingleton<CustomMouseKeyMapping>();
           
        }

        protected override void OnUpdate()
        {
            _camera = Camera.main;
            if (_camera == null) return;
            CheckMouseEventAndRaycastHit();
            
        }
       

        /// <summary>
        /// Only Detect Clickable Layer = Terrain + other gameplay layers
        /// </summary>
        /// <returns></returns>
        private void CheckMouseEventAndRaycastHit()
        {
            _isDoubleClick = false;
            
            var data = new InputMouseData
            {
                ClickFlag = ClickFlag.None,
                ClickType = ClickType.None,
                IsOverUI = false,
                HitEntity = Entity.Null,
                HitPosition = float3.zero,
                MousePosition = float3.zero,
            };
            if (EventSystem.current.IsPointerOverGameObject())
                data.IsOverUI = true;
            data.MousePosition = Input.mousePosition;
            if (MouseCastOnEntity(out var entity, out var hitPosition))
            {
                data.HitEntity = entity;
                data.HitPosition = hitPosition;
            }

            if (Input.GetMouseButtonDown(_mouseKeyMapping.LeftClickIndex))
            {
                if (_currentClickInterval < _config.DoubleClickThreshold)
                {
                    _isDoubleClick = true;
                }

                data.ClickType = ClickType.Left;
                data.ClickFlag = ClickFlag.Start;
            }

            if (Input.GetMouseButton(_mouseKeyMapping.LeftClickIndex))
            {
                data.ClickType = ClickType.Left;
                data.ClickFlag = (data.ClickFlag == ClickFlag.Start) ? ClickFlag.Start : ClickFlag.Clicking;
            }

            if (Input.GetMouseButtonUp(_mouseKeyMapping.LeftClickIndex))
            {
                data.ClickType = ClickType.Left;
                data.ClickFlag = ClickFlag.End;
            }

            if (Input.GetMouseButtonDown(_mouseKeyMapping.RightClickIndex))
            {
                if (_currentClickInterval < _config.DoubleClickThreshold)
                {
                    _isDoubleClick = true;
                }

                data.ClickType = ClickType.Right;
                data.ClickFlag = ClickFlag.Start;
            }

            if (Input.GetMouseButton(_mouseKeyMapping.RightClickIndex))
            {
                data.ClickType = ClickType.Right;
                data.ClickFlag = (data.ClickFlag == ClickFlag.Start) ? ClickFlag.Start : ClickFlag.Clicking;
            }

            if (Input.GetMouseButtonUp(_mouseKeyMapping.RightClickIndex))
            {
                data.ClickType = ClickType.Right;
                data.ClickFlag = ClickFlag.End;
            }

            if (Input.GetMouseButtonDown(2))
            {
                data.ClickType = ClickType.Middle;
                data.ClickFlag = ClickFlag.Start;
                if (MouseCastOnGroundPlane(out var hitGroundPos))
                {
                    data.HitPosition = hitGroundPos;
                }
            }

            if (Input.GetMouseButton(2))
            {
                data.ClickType = ClickType.Middle;
                data.ClickFlag = data.ClickFlag == ClickFlag.Start ? ClickFlag.Start : ClickFlag.Clicking;
                if (MouseCastOnGroundPlane(out var hitGroundPos))
                {
                    data.HitPosition = hitGroundPos;
                }
            }

            if (Input.GetMouseButtonUp(2))
            {
                data.ClickType = ClickType.Middle;
                data.ClickFlag = ClickFlag.End;
            }

            // Check if double click
            if (data.ClickFlag != ClickFlag.Start)
            {
                _currentClickInterval += SystemAPI.Time.DeltaTime;
                _currentClickInterval = math.min(10000, _currentClickInterval);
            }
            else
            {
                data.ClickFlag = _isDoubleClick ? ClickFlag.DoubleClick : ClickFlag.Start;
                _currentClickInterval = 0;
            }
            SystemAPI.SetSingleton(data);
        }

        #region MathRayCast

        private bool MouseCastOnEntity(out Entity hitEntity, out float3 hitPosition)
        {
            var camRay = _camera.ScreenPointToRay(Input.mousePosition);
            float3 rayStart = camRay.origin;
            var rayEnd = rayStart + (float3)camRay.direction * _config.RaycastDistance;
            var raycastInput = new RaycastInput
            {
                Start = rayStart,
                End = rayEnd,
                Filter = new CollisionFilter
                {
                    BelongsTo = _config.MouseRayLayerMask,
                    CollidesWith = _config.ClickableLayerMask,
                    GroupIndex = 0
                }
            };
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            if (physicsWorld.PhysicsWorld.CollisionWorld.CastRay(raycastInput, out var raycastHit))
            {
                hitEntity = physicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
                hitPosition = raycastHit.Position;
                if (EntityManager.Exists(hitEntity))
                {
                    return true;
                }
            }

            hitEntity = Entity.Null;
            hitPosition = float3.zero;
            return false;
        }

        private bool MouseCastOnGroundPlane(out float3 hitPosition)
        {
            var camRay = _camera.ScreenPointToRay(Input.mousePosition);
            if (PlaneRaycast(camRay.origin, camRay.direction, out hitPosition))
            {
                return true;
            }

            hitPosition = float3.zero;
            return false;
        }

        private static bool PlaneRaycast(in float3 rayOrigin, in float3 rayDirection, out float3 hitPoint)
        {
            var planeNormal = new float3(0f, 1f, 0f);
            var planePoint = float3.zero;

            var cosTheta = math.dot(rayDirection, planeNormal);
            // If not parallel to plane
            if (math.abs(cosTheta) > 1e-6f)
            {
                var t = math.dot(planePoint - rayOrigin, planeNormal) / cosTheta;
                if (t >= 0f)
                {
                    hitPoint = rayOrigin + rayDirection * t;
                    return true;
                }
            }

            hitPoint = float3.zero;
            return false;
        }

        #endregion
    }
}