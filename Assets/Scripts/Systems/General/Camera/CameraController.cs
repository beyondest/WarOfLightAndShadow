// using UnityEngine;
//
// namespace SparFlame.Systems.General.Camera
// {
//     public class CameraController : MonoBehaviour
//     {
//         public UnityEngine.Camera subGameCamera;
//         public UnityEngine.Camera mainGameCamera;
//
//         public GameObject subGameCameraGo;
//        public GameObject mainGameCameraGo;
//         public static CameraController Instance;
//
//         private void Awake()
//         {
//             if (!Instance)
//             {
//                 Instance = this;
//             }
//             else
//             {
//                 Destroy(gameObject);
//             }
//         }
//
//         private void Start()
//         {
//             subGameCameraGo.SetActive(false);
//             mainGameCameraGo.SetActive(true);
//         }
//     }
// }