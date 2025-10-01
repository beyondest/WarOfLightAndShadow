using System;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Entities;
using System.Collections;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Collections;
using UnityEngine.AI;

namespace SparFlame.Systems.SubGameplay.Movement
{
    public class NavMeshControllerSubGameplay : MonoBehaviour
    {
        public NavMeshSurface navAlly;
        public NavMeshSurface navEnemy;

        [Tooltip("Can only update once in min update interval")]
        public float minUpdateInterval = 1f;

        // Internal data
        private float _nextUpdateTime;
        private bool _isUpdatingAlly;
        private bool _isUpdatingEnemy;
        private bool _pendingUpDataAlly;
        private bool _pendingUpDateEnemy;

        // ECS
        private EntityManager _em;
        private EntityQuery _updateNavMeshRequest;

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (_updateNavMeshRequest != default)
            {
                _updateNavMeshRequest.Dispose();
            }
            _updateNavMeshRequest = _em.CreateEntityQuery(typeof(UpdateNavMeshRequest),typeof(SubGameplayEntityTag));
        }

        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.Return))
            // {
            //     StartCoroutine(UpdateNavMesh(FactionTag.Ally));
            // }

            // If game pause, do nothing
            var ifUpdateAlly = false;
            var ifUpdateEnemy = false;

            var currentTime = Time.time;
            if (currentTime < _nextUpdateTime) return;
                
            var componentDataArray = _updateNavMeshRequest.ToComponentDataArray<UpdateNavMeshRequest>(Allocator.Temp);
            var entities = _updateNavMeshRequest.ToEntityArray(Allocator.Temp);
            if (componentDataArray.Length == 0) return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (var i = 0; i < componentDataArray.Length; i++)
            {
                var updateNavMeshRequest = componentDataArray[i];
                switch (updateNavMeshRequest.FactionTag)
                {
                    case FactionTag.Light:
                    {
                        if (_isUpdatingAlly)
                        {
                            _pendingUpDataAlly = true;
                            continue;
                        }
                        ifUpdateAlly = true;
                        break;
                    }
                    case FactionTag.Dark:
                    {
                        if (_isUpdatingEnemy)
                        {
                            _pendingUpDateEnemy = true;
                            continue;
                        }
                        ifUpdateEnemy = true;
                        break;
                    }
                    case FactionTag.Neutral:
                    default:
                        break;
                }
                ecb.DestroyEntity(entities[i]);
                _nextUpdateTime = Time.time + minUpdateInterval;
            }
            ecb.Playback(_em);
            ecb.Dispose();
            if (ifUpdateAlly )
            {
                StartCoroutine(UpdateNavMesh(FactionTag.Light));
            }
            else
            {
                if (_pendingUpDataAlly)
                {
                    _pendingUpDataAlly = false;
                    StartCoroutine(UpdateNavMesh(FactionTag.Light));
                }
            }
            if (ifUpdateEnemy )
            {
                StartCoroutine(UpdateNavMesh(FactionTag.Dark));
            }
            else
            {
                if (_pendingUpDateEnemy)
                {
                    _pendingUpDateEnemy = false;
                    StartCoroutine(UpdateNavMesh(FactionTag.Dark));
                }
            }
        }

        private void OnDestroy()
        {
            if (_updateNavMeshRequest != default)
                _updateNavMeshRequest.Dispose();
        }

        private IEnumerator UpdateNavMesh(FactionTag factionTag)
        {
            switch (factionTag)
            {
                case FactionTag.Light:
                    _isUpdatingAlly = true;
                    break;
                case FactionTag.Dark:
                    _isUpdatingEnemy = true;
                    break;
                case FactionTag.Neutral:
                default:
                    break;
            }

            var operation = factionTag == FactionTag.Light
                ? navAlly.UpdateNavMesh(navAlly.navMeshData)
                : navEnemy.UpdateNavMesh(navEnemy.navMeshData);


            while (!operation.isDone)
            {
                yield return null;
            }

            switch (factionTag)
            {
                case FactionTag.Light:
                    _isUpdatingAlly = false;
                    break;
                case FactionTag.Dark:
                    _isUpdatingEnemy = false;
                    break;
                case FactionTag.Neutral:
                default:
                    break;
            }
        }
    }
}