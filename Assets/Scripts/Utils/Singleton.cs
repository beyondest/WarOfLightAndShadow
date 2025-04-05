// using UnityEngine;
//
// // ReSharper disable StaticMemberInGenericType
//
// public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
// {
//
//
//     public static T Instance;
//  
//     protected virtual bool PersistThroughScenes => false;
//
//     protected virtual void Awake()
//     {
//         if (Instance == null)
//         {
//             // Instance = gameObject.GetComponent<T>();
//             Instance = this as T;
//             if (PersistThroughScenes)
//             {
//                 DontDestroyOnLoad(gameObject);
//             }
//         }
//         else/* if (Instance.GetInstanceID() != GetInstanceID())*/
//         {
//             Destroy(gameObject);
//             Debug.LogError($"[{typeof(T)}] Instance already exists! Destroying duplicate: {gameObject.name}");
//         }
//     }
//     
//     // private static readonly object InstanceLock = new();
//     // private static bool _quitting;
//
//     // public static T Instance;
//     // {
//     //     get
//     //     {
//     //         lock (InstanceLock)
//     //         {
//     //             if (_instance == null && !_quitting)
//     //             {
//     //                 _instance = FindAnyObjectByType<T>();
//     //                 if (_instance == null)
//     //                 {
//     //                     var go = new GameObject(typeof(T).ToString());
//     //                     _instance = go.AddComponent<T>();
//     //                 }
//     //             }
//     //             return _instance;
//     //         }
//     //     }
//     // }
//
//
//
//     // protected virtual void OnApplicationQuit()
//     // {
//     //     _quitting = true;
//     // }
// }