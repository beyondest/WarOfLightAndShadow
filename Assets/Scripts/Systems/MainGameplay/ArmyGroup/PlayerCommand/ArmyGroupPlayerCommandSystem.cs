using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupPlayerCommandSystem : ISystem
    {
        private ComponentLookup<ArmyGroupMovingTag> _armyGroupMovingTagLookup;
        private NativeList<Entity> _flags;
        private ComponentLookup<GlobalSingleId> _singleIdLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainGameplayCursorData>();
            state.RequireForUpdate<ArmyGroupSelectionData>();
            state.RequireForUpdate<ArmyGroupCommandSystemConfig>();
            state.RequireForUpdate<CameraData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<InputArmyGroupControlData>();
            state.RequireForUpdate<MainGamingTag>();
            _armyGroupMovingTagLookup = state.GetComponentLookup<ArmyGroupMovingTag>(true);
            _singleIdLookup = state.GetComponentLookup<GlobalSingleId>(true);
            _flags = new NativeList<Entity>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_flags.IsCreated)
                _flags.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inputArmyGroupData = SystemAPI.GetSingleton<InputArmyGroupControlData>();
            var cursorData = SystemAPI.GetSingleton<MainGameplayCursorData>();
            var config = SystemAPI.GetSingleton<ArmyGroupCommandSystemConfig>();
            var armyGroupSelectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            
            // Clear flags if no army group selected
            if (armyGroupSelectionData.CurrentSelectCount <= 0)
            {
                if (!_flags.IsEmpty)
                {
                    foreach (var flag in _flags)
                    {
                        state.EntityManager.DestroyEntity(flag);
                    }

                    _flags.Clear();
                }

                return;
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var ecbP = ecb.AsParallelWriter();


            #region Check army group command shortcut

            

            // Set army group moving target
            if (inputArmyGroupData.SetTarget)
            {
                var flag = state.EntityManager.Instantiate(config.FlagPrefab);
                var trans = SystemAPI.GetComponent<LocalTransform>(config.FlagPrefab);
                trans.Position = inputMouseData.HitPosition;
                state.EntityManager.AddComponent<MainGameplayEntityTag>(flag);
                state.EntityManager.SetComponentData(flag, trans);
                _flags.Add(flag);
                _armyGroupMovingTagLookup.Update(ref state);
                _singleIdLookup.Update(ref state);
                var targetPosition =inputMouseData.HitPosition;
                var targetEntity = Entity.Null;
                var targetState = ArmyGroupState.Idle;
                var setTargetValid = false;
                var targetBoxColliderSizeXz = float2.zero;
                switch (cursorData.CursorType)
                {
                    case MainGameplayCursorType.CheckInfo:
                    case MainGameplayCursorType.None:
                        break;
                    case MainGameplayCursorType.March:
                        setTargetValid = true;
                        break;
                    case MainGameplayCursorType.Garrison:
                        setTargetValid = true;
                        targetState = ArmyGroupState.Garrison;
                        targetEntity = inputMouseData.HitEntity;
                        targetPosition = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                        targetBoxColliderSizeXz =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Box.xz;
                        break;
                    case MainGameplayCursorType.Support:
                        setTargetValid = true;
                        targetState = ArmyGroupState.Support;
                        targetEntity = inputMouseData.HitEntity;
                        targetPosition = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                        targetBoxColliderSizeXz =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Box.xz;
                        break;
                    case MainGameplayCursorType.Invade:
                        setTargetValid = true;
                        targetState = ArmyGroupState.Invade;
                        targetEntity = inputMouseData.HitEntity;
                        targetPosition = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                        targetBoxColliderSizeXz =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Box.xz;
                        break;
                    case MainGameplayCursorType.Intercept:
                        setTargetValid = true;
                        targetState = ArmyGroupState.Idle;
                        targetEntity = Entity.Null;
                        targetPosition = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                        break;
                }

                if (setTargetValid)
                {
                    new ArmyGroupSetTargetJob
                    {
                        ArmyGroupMovingTagLookup = _armyGroupMovingTagLookup,
                        SingleIDLookup =  _singleIdLookup,
                        ECB = ecbP,
                        TargetPosition = targetPosition,
                        TargetState = targetState,
                        TargetEntity = targetEntity,
                        TargetBoxColliderSizeXz = targetBoxColliderSizeXz
                    }.ScheduleParallel();
                }
            }


            // Set selected army group start moving
            if (inputArmyGroupData.StartMoving)
            {
                new ArmyGroupStartMovingJob
                {
                    ECB = ecbP
                }.ScheduleParallel();
                var pos = SystemAPI.GetSingleton<CameraData>().CameraRigPosition;
                AudioUtils.PlayAudioClip(AudioName.ArmyGroupStartMoving, pos, state.EntityManager);
            }

            if (inputArmyGroupData.ClearAllTargets)
            {
                foreach (var flag in _flags)
                {
                    ecb.DestroyEntity(flag);
                }

                _flags.Clear();

                new ArmyGroupClearAllMovingTargetsJob
                {
                    ECB = ecb
                }.Schedule();
            }

            if (inputArmyGroupData.DeleteLastTarget)
            {
                if (_flags.Length > 0)
                {
                    ecb.DestroyEntity(_flags[_flags.Length - 1]);
                    _flags.RemoveAt(_flags.Length - 1);
                }

                new ArmyGroupDeleteLastTargetJob
                {
                    ECB = ecbP
                }.ScheduleParallel();
            }

            if (inputArmyGroupData.EndMovingAndClearAllTargets)
            {
                foreach (var flag in _flags)
                {
                    ecb.DestroyEntity(flag);
                }

                _flags.Clear();
                new ArmyGroupEndMovingJob
                {
                    ECB = ecbP
                }.ScheduleParallel();
            }
            #endregion

        }
    }
}