// using UnityEngine;
//
// public class TestNonSerializedField : MonoBehaviour
// {
//     [Header("可序列化字段 (Inspector 可见)")]
//     public int serializedValue = 10;
//
//     [Header("不可序列化字段 (Inspector 不显示)")]
//     private int nonSerializedValue = -10;
//
//     [Header("静态字段 (所有实例共享)")]
//     private static int staticValue = 100;
//
//     private void OnGUI()
//     {
//         GUILayout.Label($"[Serialized] Value = {serializedValue}");
//         GUILayout.Label($"[Non-Serialized] Value = {nonSerializedValue}");
//         GUILayout.Label($"[Static] Value = {staticValue}");
//
//         if (GUILayout.Button("修改所有值"))
//         {
//             serializedValue += 1;
//             nonSerializedValue += 1;
//             staticValue += 1;
//             Debug.Log($"值已修改: serialized={serializedValue}, nonSerialized={nonSerializedValue}, static={staticValue}");
//         }
//     }
// }