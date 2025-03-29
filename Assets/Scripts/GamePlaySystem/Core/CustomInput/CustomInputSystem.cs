using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Physics;
using SparFlame.GamePlaySystem.General;
using Unity.Transforms;
using UnityEngine.EventSystems;

namespace SparFlame.GamePlaySystem.Mouse
{
    [UpdateAfter(typeof(GameBasicControlSystem))]
    public partial class CustomInputSystem : SystemBase
    {
        private float _raycastDistance;
        private float _doubleClickThreshold;
        private uint _clickableLayerMask;
        private uint _mouseRayLayerMask;
        private int _leftClickIndex;
        private int _rightClickIndex;

        private Camera _camera;
        private float _clickThreshold;
        private bool _isDoubleClick;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<CustomInputSystemData>();
            RequireForUpdate<CustomInputSystemConfig>();
        }

        protected override void OnStartRunning()
        {
            _camera = Camera.main;
            var config = SystemAPI.GetSingleton<CustomInputSystemConfig>();
            _raycastDistance = config.RaycastDistance;
            _doubleClickThreshold = config.DoubleClickThreshold;
            _clickableLayerMask = config.ClickableLayerMask;
            _mouseRayLayerMask = config.MouseRayLayerMask;
            _leftClickIndex = config.LeftClickIndex;
            _rightClickIndex = _leftClickIndex == 0 ? 1 : 0;
        }

        protected override void OnUpdate()
        {
            _camera = Camera.main;
            if (_camera == null) return;
            var inputSystemDataEntity = SystemAPI.GetSingletonEntity<CustomInputSystemData>();
            var config = SystemAPI.GetSingleton<CustomInputSystemConfig>();
            var data = new CustomInputSystemData
            {
                AddUnit = false,
                ChangeFaction = false,
                ClickFlag = ClickFlag.None,
                ClickType = ClickType.None,
                IsOverUI = false,
                Focus = false,
                HitEntity = Entity.Null,
                HitPosition = float3.zero,
                MousePosition = float3.zero,
            };
            _isDoubleClick = false;
            if (EventSystem.current.IsPointerOverGameObject())
                data.IsOverUI = true;
            CheckMouseEventAndRaycastHit(ref data);
            CheckKey(ref data, in config);
            EntityManager.SetComponentData(inputSystemDataEntity, data);
        }

        private void CheckKey(ref CustomInputSystemData data, in CustomInputSystemConfig config)
        {
            data.ChangeFaction = Input.GetKeyDown(config.ChangeFactionKey);
            data.AddUnit = Input.GetKey(config.AddUnitKey);
            data.Focus = Input.GetKey(config.FocusKey);
        }

        /// <summary>
        /// Only Detect Clickable Layer
        /// </summary>
        /// <returns></returns>
        private void CheckMouseEventAndRaycastHit(ref CustomInputSystemData data)
        {
            data.MousePosition = Input.mousePosition;
            if (MouseCastOnEntity(out var entity, out var hitPosition))
            {
                data.HitEntity = entity;
                data.HitPosition = hitPosition;
            }

            if (Input.GetMouseButtonDown(_leftClickIndex))
            {
                if (_clickThreshold < _doubleClickThreshold)
                {
                    _isDoubleClick = true;
                }

                data.ClickType = ClickType.Left;
                data.ClickFlag = ClickFlag.Start;
            }

            if (Input.GetMouseButton(_leftClickIndex))
            {
                data.ClickType = ClickType.Left;
                data.ClickFlag = (data.ClickFlag == ClickFlag.Start) ? ClickFlag.Start : ClickFlag.Clicking;
            }

            if (Input.GetMouseButtonUp(_leftClickIndex))
            {
                data.ClickType = ClickType.Left;
                data.ClickFlag = ClickFlag.End;
            }

            if (Input.GetMouseButtonDown(_rightClickIndex))
            {
                if (_clickThreshold < _doubleClickThreshold)
                {
                    _isDoubleClick = true;
                }

                data.ClickType = ClickType.Right;
                data.ClickFlag = ClickFlag.Start;
            }

            if (Input.GetMouseButton(_rightClickIndex))
            {
                data.ClickType = ClickType.Right;
                data.ClickFlag = (data.ClickFlag == ClickFlag.Start) ? ClickFlag.Start : ClickFlag.Clicking;
            }

            if (Input.GetMouseButtonUp(_rightClickIndex))
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
                _clickThreshold += SystemAPI.Time.DeltaTime;
                _clickThreshold = math.min(10000, _clickThreshold);
            }
            else
            {
                data.ClickFlag = _isDoubleClick ? ClickFlag.DoubleClick : ClickFlag.Start;
                _clickThreshold = 0;
            }
        }


        #region MathRayCast

        private bool MouseCastOnEntity(out Entity hitEntity, out float3 hitPosition)
        {
            var camRay = _camera.ScreenPointToRay(Input.mousePosition);
            float3 rayStart = camRay.origin;
            var rayEnd = rayStart + (float3)camRay.direction * _raycastDistance;
            var raycastInput = new RaycastInput
            {
                Start = rayStart,
                End = rayEnd,
                Filter = new CollisionFilter
                {
                    BelongsTo = _mouseRayLayerMask,
                    CollidesWith = _clickableLayerMask,
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
                // Calculate distance between rayOrigin and hitPoint:
                // t = dot(planePoint - rayOrigin, planeNormal) / dot(rayDirection, planeNormal)
                // t = (Vertical Distance / Cos theta)
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