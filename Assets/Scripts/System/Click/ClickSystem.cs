using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Physics;
using SparFlame.System.General;

namespace SparFlame.System.Click
{
    public partial class ClickSystem : SystemBase
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


        protected override void OnStartRunning()
        {
            _camera = Camera.main;
            var clickSystemConfig = SystemAPI.GetSingleton<ClickSystemConfig>();
            _raycastDistance = clickSystemConfig.RaycastDistance;
            _doubleClickThreshold = clickSystemConfig.DoubleClickThreshold;
            _clickableLayerMask = clickSystemConfig.ClickableLayerMask;
            _mouseRayLayerMask = clickSystemConfig.MouseRayLayerMask;
            _leftClickIndex = clickSystemConfig.LeftClickIndex;
            _rightClickIndex = _leftClickIndex == 0 ? 1 : 0;
#if DEBUG_ClickSystem
            if (Camera.main == null)
            {
                Debug.LogError("No camera found");
            }
#endif
        }

        protected override void OnUpdate()
        {
            var clickSystemData = SystemAPI.GetSingletonEntity<ClickSystemData>();
            ClickFlag clickFlag = ClickFlag.None;
            ClickType clickType = ClickType.None;
            Entity hitEntity = Entity.Null;
            float3 hitPosition = float3.zero;
            float3 mousePosition = Input.mousePosition;
            _isDoubleClick = false;

            CheckMouseInput(ref clickFlag, ref clickType, ref hitEntity, ref hitPosition, ref mousePosition);

            EntityManager.SetComponentData(clickSystemData, new ClickSystemData
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
        /// <param name="hitEntity"></param>
        /// <param name="hitPosition"></param>
        /// <returns></returns>
        private bool MouseCastOnEntity(out Entity hitEntity, out float3 hitPosition)
        {
            UnityEngine.Ray camRay = _camera.ScreenPointToRay(Input.mousePosition);
            float3 rayStart = camRay.origin;
            float3 rayEnd = rayStart + (float3)camRay.direction * _raycastDistance;
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
                if (EntityManager.Exists(hitEntity) && EntityManager.HasComponent<BasicAttributes>(hitEntity))
                {
                    return true;
                }
#if DEBUG_ClickSystem
                else
                {
                    Debug.Log("Entity not exist , or entity has no attribute component");
                }
#endif
            }

            hitEntity = Entity.Null;
            hitPosition = float3.zero;
            return false;
        }

        private void CheckMouseInput(ref ClickFlag clickFlag, ref ClickType clickType, ref Entity hitEntity,
            ref float3 hitPos, ref float3 mousePosition)
        {
            if (Input.GetMouseButtonDown(_leftClickIndex))
            {
                if (_clickThreshold < _doubleClickThreshold)
                {
                    _isDoubleClick = true;
                }

                clickType = ClickType.Left;
                clickFlag = ClickFlag.Start;
                if (MouseCastOnEntity(out Entity entity, out float3 hitPosition))
                {
                    hitEntity = entity;
                    hitPos = hitPosition;
                }
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
                if (MouseCastOnEntity(out Entity entity, out float3 hitPosition))
                {
                    hitEntity = entity;
                    hitPos = hitPosition;
                }
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

            if (Input.GetMouseButton(2))
            {
                clickType = ClickType.Middle;
                clickFlag = ClickFlag.Clicking;
            }

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
    }
}