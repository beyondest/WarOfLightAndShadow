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
        
        private float _nextUpdateTime;
        private EntityManager _em;
        private bool _isUpdatingAlly;
        private bool _isUpdatingEnemy;
        
        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartCoroutine(UpdateNavMesh(FactionTag.Ally));
                Debug.Log("Fuck");
            }
            // If game pause, do nothing
            if (!_em.CreateEntityQuery(typeof(NotPauseTag)).TryGetSingletonEntity< NotPauseTag>(out var _))
            {
                return;
            }
            var ifUpdateAlly = false;
            var ifUpdateEnemy = false;
            
            var currentTime = Time.time;
            if(currentTime < _nextUpdateTime)return;
            var query = _em.CreateEntityQuery(typeof(UpdateNavMeshRequest));
            var componentDataArray = query.ToComponentDataArray<UpdateNavMeshRequest>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            if (componentDataArray.Length == 0) return;
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            
            for (var i = 0; i < componentDataArray.Length; i++)
            {
                var updateNavMeshRequest = componentDataArray[i];
                switch (updateNavMeshRequest.FactionTag)
                {
                    case FactionTag.Ally:
                    {
                        if(_isUpdatingAlly)continue;
                        ifUpdateAlly = true;
                        break;
                    }
                    case FactionTag.Enemy:
                    {
                        if(_isUpdatingEnemy)continue;
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
            if (ifUpdateAlly) StartCoroutine(UpdateNavMesh(FactionTag.Ally));
            if (ifUpdateEnemy) StartCoroutine(UpdateNavMesh(FactionTag.Enemy));
            
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