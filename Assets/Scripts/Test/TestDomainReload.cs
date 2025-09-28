// using System;
// using UnityEngine;
//
// namespace SparFlame.Test
// {
//
// // 总结：
// // 
// // 对于可序列化字段，无论是否 domain reload :
// //          在非编辑器模式下，无论是mono还是so，每一次修改都会保留到playmode，即使没有保存（因为有native快照，不需要写入磁盘）
// //          在playmode下：对于monobehaviour这种每次play都会重新构建的非asset对象，每次修改都会在exit playmode时重置为进入playmode之前的值
// //                       对于scriptable object这种asset对象，playmode下的修改会保存起来，exit playmode不会恢复
// // 对于不可序列化字段：
// //        无论是否domain reload，对于mono这种每次play都要重建的对象，其值都会重置为默认值
// //        对于so这种asset对象，如果启用domain reload，值会重置为默认值，否则，会一直延续上次的修改，即使它不是静态变量
// // 对于静态字段、静态事件：
// //          只有domain reload或者手动代码清空可以清除
//     [ExecuteAlways]
//     public class TestDomainReload : MonoBehaviour
//     {
//         // 可序列化字段（会显示在 Inspector）
//         public int serializedValue = 10;
//
//
//         // 静态字段（仅受 Domain Reload 控制）
//         private static int _staticValue = 0;
//
//         // 引用一个 ScriptableObject 资产（把 TestSO 拖到这里）
//         public TestSO testSO;
//
//         private void Awake()
//         {
//             Debug.Log(
//                 $"[Awake] serialized={serializedValue}, nonSerialized={testSO.testvalue}, static={_staticValue}, so={testSO?.soValue}");
//         }
//
//         private void OnEnable()
//         {
//             Debug.Log("Enable");
//             Check();
//         }
//
//         private void Update()
//         {
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 Check();
//             }
//         }
//
//         private void OnDisable()
//         {
//             Debug.Log("Disable");
//             Check();
//         }
//
//         private void Check()
//         {
//             serializedValue++;
//             testSO.testvalue++;
//             _staticValue++;
//             if (testSO != null) testSO.soValue++;
//
//             Debug.Log(
//                 $"[Update] serialized={serializedValue}, nonSerialized={testSO.testvalue}, static={_staticValue}, so={testSO?.soValue}");
//         }
//     }
// }