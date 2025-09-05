// using UnityEngine;
//
// namespace SparFlame.GamePlaySystem.Fow
// {
//     public class FogOfWarGo : MonoBehaviour
//     {
//         
//         public static FogOfWarGo Instance;
//          [SerializeField] [Tooltip("(Essential) FOV map Texture2DArray for runtime FOV mapping")]
//         public Texture2DArray fovMapArray;
//
//          [SerializeField] [Tooltip("(Do not modify) FOV mapping shader")]
//         public Shader fovShader;
//
//        [SerializeField] [Tooltip("(Do not modify) Fog of war projector shader")]
//         public Shader fowProjectorShader;
//
//        [SerializeField] [Tooltip("(Do not modify) Gaussian filter shader")]
//         public Shader gaussianShader;
//
//         [SerializeField] [Tooltip("(Do not modify) Pixel reader computer shader")]
//         public ComputeShader pixelReader;
//
//         private void Awake()
//         {
//             if(Instance == null)
//                 Instance = this;
//             else
//             {
//                 Destroy(gameObject);
//             }
//         }
//
//         public void SetMaterial(Material fovMaterial)
//         {
//             GetComponent<MeshRenderer>().material = fovMaterial;
//         }
//     }
// }