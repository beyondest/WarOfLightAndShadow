// using System;
// using System.IO;
// using System.Threading.Tasks;
// using SparFlame.Systems.General.BasicControl;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Test
// {
//     public class SaveManager : MonoBehaviour
//     {
//         public event Action OnLoadComplete;
//         public static SaveManager Instance;
//
//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//             }
//             else
//             {
//                 Destroy(gameObject);
//             }
//         }
//
//         private void Update()
//         {
//             if (Input.GetKeyDown(KeyCode.S))
//             {
//                 Save();
//             }
//
//             if (Input.GetKeyDown(KeyCode.L))
//             {
//                 Load();
//             }
//         }
//
//         private void Save()
//         {
//             Debug.Log("Start saving");
//             var config = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(TestSavingConfig))
//                 .GetSingleton<TestSavingConfig>();
//             var savePath = SaveUtilities.GetCityMainDataPath(3);
//             var savings = new NativeList<TestSavingComponentA>(Allocator.Temp);
//             for (var i = 0; i < config.savingComponentCount; i++)
//             {
//                 savings.Add(new TestSavingComponentA() { Value = i });
//             }
//
//             var bytes = savings.Length * UnsafeUtility.SizeOf<TestSavingComponentA>();
//             var managed = savings.AsArray().Reinterpret<byte>(UnsafeUtility.SizeOf<TestSavingComponentA>()).ToArray();
//             savings.Dispose();
//             var dir = Path.GetDirectoryName(savePath);
//             if (dir != null && !Directory.Exists(dir))
//             {
//                 Directory.CreateDirectory(dir);
//             }
//
//             if (dir == null || !Directory.Exists(dir))
//             {
//                 Debug.LogError("Fuck");
//                 return;
//             }
//
//             var tmp = savePath + ".tmp";
//             using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
//             {
//                 fs.Write(managed, 0, managed.Length);
//                 fs.Flush();
//             }
//
//             File.Copy(tmp, savePath, true);
//         }
//
//
//         private void Load()
//         {
//             Debug.Log("Start loading");
//             var savePath = SaveUtilities.GetCityMainDataPath(3);
//
//             var managed = File.ReadAllBytes(savePath);
//
//             int sizeOf = UnsafeUtility.SizeOf<TestSavingComponentA>();
//             int count = managed.Length / sizeOf;
//
//             var native = new NativeArray<TestSavingComponentA>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
//
//             unsafe
//             {
//                 fixed (byte* src = managed)
//                 {
//                     UnsafeUtility.MemCpy(native.GetUnsafePtr(), src, managed.Length);
//                 }
//             }
//
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             foreach (var s in native)
//             {
//                 var entity = ecb.CreateEntity();
//                 ecb.AddComponent(entity, s);
//             }
//
//             ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
//             ecb.Dispose();
//             native.Dispose();
//
//             OnLoadComplete?.Invoke();
//         }
//
//     }
// }