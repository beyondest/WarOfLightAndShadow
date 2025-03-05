// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Test
// {
//     public class GC : MonoBehaviour
//     {
//         EntityManager _entityManager;
//         private void Start()
//         {
//             _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
//         }
//
//         private void Update()
//         {
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 Time.timeScale = 0;
//                 var e = _entityManager.CreateEntityQuery(typeof(TestAttrData));
//                 if (e.TryGetSingletonEntity<TestAttrData>(out var entity))
//                 {
//                     _entityManager.SetEnabled(entity, false);
//                     
//                     //This line does not work, require update will update even component disable
//                     //_entityManager.SetComponentEnabled<PauseData>(entity, false);
//                 } }
//
//             if (Input.GetKeyDown(KeyCode.Return))
//             {
//                 Time.timeScale = 1;
//                 
//                 //var e = _entityManager.CreateEntityQuery(typeof(TestAttrData));
//                 var e = _entityManager.CreateEntityQuery(new EntityQueryDesc
//                 {
//                     All = new ComponentType[] { typeof(TestAttrData) },
//                     Options = EntityQueryOptions.IncludeDisabledEntities // 允许查找被禁用的组件
//                 });
//                 if (e.TryGetSingletonEntity<TestAttrData>(out var entity))
//                 {
//                     _entityManager.SetEnabled(entity, true);
//                 }
//             }
//         }
//     }
// }