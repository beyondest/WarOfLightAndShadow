// using UnityEngine;
// using UnityEditor;
//
// namespace Editor
// {
//     
// public class NullMeshDetector : MonoBehaviour
// {
//     [MenuItem("Tools/Check/Check for Null Mesh or Material")]
//     static void CheckForNulls()
//     {
//         var renderers = GameObject.FindObjectsOfType<MeshRenderer>(true);
//         foreach (var r in renderers)
//         {
//             var mf = r.GetComponent<MeshFilter>();
//             if (mf == null || mf.sharedMesh == null)
//                 Debug.LogWarning($"Null Mesh on GameObject: {r.gameObject.name}", r.gameObject);
//
//             var mats = r.sharedMaterials;
//             for (int i = 0; i < mats.Length; i++)
//             {
//                 if (mats[i] == null)
//                     Debug.LogWarning($"Null Material at index {i} on GameObject: {r.gameObject.name}", r.gameObject);
//             }
//         }
//
//         var skinned = GameObject.FindObjectsOfType<SkinnedMeshRenderer>(true);
//         foreach (var r in skinned)
//         {
//             if (r.sharedMesh == null)
//                 Debug.LogWarning($"Null Skinned Mesh on GameObject: {r.gameObject.name}", r.gameObject);
//
//             var mats = r.sharedMaterials;
//             for (int i = 0; i < mats.Length; i++)
//             {
//                 if (mats[i] == null)
//                     Debug.LogWarning($"Null Material at index {i} on GameObject: {r.gameObject.name}", r.gameObject);
//             }
//         }
//     }
// }}
