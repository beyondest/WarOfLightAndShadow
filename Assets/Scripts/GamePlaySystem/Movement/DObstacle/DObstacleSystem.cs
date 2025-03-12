using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public partial class DObstacleSystem : SystemBase
    {
        private Dictionary<Entity, GameObject> _obstacleMap = new Dictionary<Entity, GameObject>();
        private Dictionary<DObstacleType, GameObject> _typePrefabMap;
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<DObstacleTypePrefabMap>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            foreach (var map in SystemAPI.Query<DObstacleTypePrefabMap>())
            {
                _typePrefabMap = map.TypePrefabMap;
            }
        }

        protected override void OnUpdate()
        {
            foreach (var (_, entity) in SystemAPI.Query<RefRO<DObstacleTag>>().WithEntityAccess())
            {
                if (!_obstacleMap.TryGetValue(entity, out var go1))
                {
                    Debug.Log("Instantiate prefab");
                    var go = Object.Instantiate(_typePrefabMap[DObstacleType.Unit], Vector3.zero, Quaternion.identity);
                    _obstacleMap.Add(entity, go);
                }
                else
                {
                    Debug.Log("Move obstacle");
                    go1.transform.position += Vector3.left * 0.01f;
                    if (!Input.GetKeyDown(KeyCode.D)) continue;
                    _obstacleMap.Remove(entity);
                    Object.Destroy(go1);
                }
            }
        }
    }
}