using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Physics;
using SparFlame.GamePlaySystem.General;

namespace SparFlame.GamePlaySystem.Mouse
{
    public partial class MouseSystem : SystemBase
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
            RequireForUpdate<MouseSystemData>();
            RequireForUpdate<MouseSystemConfig>();
        }

        protected override void OnStartRunning()
        {
            _camera = Camera.main;
            var clickSystemConfig = SystemAPI.GetSingleton<MouseSystemConfig>();
            _raycastDistance = clickSystemConfig.RaycastDistance;
            _doubleClickThreshold = clickSystemConfig.DoubleClickThreshold;
            _clickableLayerMask = clickSystemConfig.ClickableLayerMask;
            _mouseRayLayerMask = clickSystemConfig.MouseRayLayerMask;
            _leftClickIndex = clickSystemConfig.LeftClickIndex;
            _rightClickIndex = _leftClickIndex == 0 ? 1 : 0;

        }

        protected override void OnUpdate()
        {
            _camera = Camera.main;
            if(_camera == null)return;
            var clickSystemData = SystemAPI.GetSingletonEntity<MouseSystemData>();
            var clickFlag = ClickFlag.None;
            var clickType = ClickType.None;
            var hitEntity = Entity.Null;
            var hitPosition = float3.zero;
            float3 mousePosition = Input.mousePosition;
            _isDoubleClick = false;

            CheckMouse(ref clickFlag, ref clickType, ref hitEntity, ref hitPosition, ref mousePosition);

            EntityManager.SetComponentData(clickSystemData, new MouseSystemData
            {
                ClickFlag = clickFlag,
                ClickType = clickType,
                HitEntity = hitEntity,
                HitPosition = hitPosition,
                MousePosition = mousePosition
            });
        }

        /// <summary>
        /// Only Detect Clickable Layer
        /// </summary>
        /// <returns></returns>
        private void CheckMouse(ref ClickFlag clickFlag, ref ClickType clickType, ref Entity hitEntity,
            ref float3 hitPos, ref float3 mousePosition)
        {
            if (MouseCastOnEntity(out var entity, out var hitPosition))
            {
                hitEntity = entity;
                hitPos = hitPosition;
            }
            if (Input.GetMouseButtonDown(_leftClickIndex))
            {
                if (_clickThreshold < _doubleClickThreshold)
                {
                    _isDoubleClick = true;
                }

                clickType = ClickType.Left;
                clickFlag = ClickFlag.Start;
                
            }

            if (Input.GetMouseButton(_leftClickIndex))
            {
                clickType = ClickType.Left;
                clickFlag = (clickFlag == ClickFlag.Start) ? ClickFlag.Start : ClickFlag.Clicking;
            }

            if (Input.GetMouseButtonUp(_leftClickIndex))
            {
                clickType = ClickType.Left;
                clickFlag = ClickFlag.End;
            }

            if (Input.GetMouseButtonDown(_rightClickIndex))
            {
                if (_clickThreshold < _doubleClickThreshold)
                {
                    _isDoubleClick = true;
                }
                clickType = ClickType.Right;
                clickFlag = ClickFlag.Start;
            }

            if (Input.GetMouseButton(_rightClickIndex))
            {
                clickType = ClickType.Right;
                clickFlag = (clickFlag == ClickFlag.Start) ? ClickFlag.Start : ClickFlag.Clicking;
            }

            if (Input.GetMouseButtonUp(_rightClickIndex))
            {
                clickType = ClickType.Right;
                clickFlag = ClickFlag.End;
            }

            if (Input.GetMouseButtonDown(2))
            {
                clickType = ClickType.Middle;
                clickFlag = ClickFlag.Start;
                if (MouseCastOnGroundPlane(out var hitGroundPos))
                {
                    hitPos = hitGroundPos;
                }
            }
            
            if (Input.GetMouseButton(2))
            {
                clickType = ClickType.Middle;
                clickFlag =clickFlag == ClickFlag.Start? ClickFlag.Start : ClickFlag.Clicking;
                if (MouseCastOnGroundPlane(out var hitGroundPos))
                {
                    hitPos = hitGroundPos;
                }
            }

            if (Input.GetMouseButtonUp(2))
            {
                clickType = ClickType.Middle;
                clickFlag = ClickFlag.End;
            }
            
            // Check if double click
            if (clickFlag != ClickFlag.Start)
            {
                _clickThreshold += SystemAPI.Time.DeltaTime;
                _clickThreshold = math.min(10000, _clickThreshold);
            }
            else
            {
                clickFlag = _isDoubleClick ? ClickFlag.DoubleClick : ClickFlag.Start;
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
        
        private static bool PlaneRaycast(in float3 rayOrigin,in float3 rayDirection, out float3 hitPoint)
        {
            var planeNormal = new float3(0f, 1f, 0f);
            var planePoint  = float3.zero;

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