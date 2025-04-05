using UnityEngine;
using Unity.AI.Navigation;
using Unity.Entities;
using System.Collections;
using SparFlame.GamePlaySystem.General;
using Unity.Collections;

namespace SparFlame.GamePlaySystem.Movement
{
    public class NavMeshController : MonoBehaviour
    {
        public NavMeshSurface navAlly;
        public NavMeshSurface navEnemy;

        [Tooltip("Can only update once in min update interval")]
        public float minUpdateInterval = 1f;

        // Internal data
        private float _nextUpdateTime;
        private bool _isUpdatingAlly;
        private bool _isUpdatingEnemy;
        // private bool _needUpdateAlly;
        // private bool _needUpdateEnemy;

        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _updateNavMeshRequest;

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _updateNavMeshRequest = _em.CreateEntityQuery(typeof(UpdateNavMeshRequest));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartCoroutine(UpdateNavMesh(FactionTag.Ally));
            }

            // If game pause, do nothing
            if (_notPauseTag.IsEmpty) return;
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
                    case FactionTag.Ally:
                    {
                        if (_isUpdatingAlly)
                        {
                            // _needUpdateAlly = true;
                            continue;
                        };
                        ifUpdateAlly = true;
                        break;
                    }
                    case FactionTag.Enemy:
                    {
                        if (_isUpdatingEnemy)
                        {
                            // _needUpdateEnemy = true;
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
                // _needUpdateAlly = ifUpdateAlly && _needUpdateAlly;
                StartCoroutine(UpdateNavMesh(FactionTag.Ally));
            }
            if (ifUpdateEnemy )
            {
                // _needUpdateEnemy = ifUpdateEnemy && _needUpdateEnemy;
                StartCoroutine(UpdateNavMesh(FactionTag.Enemy));
            }
        }

        private IEnumerator UpdateNavMesh(FactionTag factionTag)
        {
            switch (factionTag)
            {
                case FactionTag.Ally:
                    _isUpdatingAlly = true;
                    Debug.Log($"Update NavMesh Ally according to Request");
                    break;
                case FactionTag.Enemy:
                    _isUpdatingEnemy = true;
                    Debug.Log($"Update NavMesh Enemy according to Request");
                    break;
                case FactionTag.Neutral:
                default:
                    break;
            }

            var operation = factionTag == FactionTag.Ally
                ? navAlly.UpdateNavMesh(navAlly.navMeshData)
                : navEnemy.UpdateNavMesh(navEnemy.navMeshData);


            while (!operation.isDone)
            {
                yield return null;
            }

            switch (factionTag)
            {
                case FactionTag.Ally:
                    _isUpdatingAlly = false;
                    break;
                case FactionTag.Enemy:
                    _isUpdatingEnemy = false;
                    break;
                case FactionTag.Neutral:
                default:
                    break;
            }
        }
    }
}