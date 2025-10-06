// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
// using GamePlaySystem.Functionality.MainGameplay.City;
// using SparFlame.Components.General;
//
// namespace Editor
// {
//     public class GridIndicatorWindow : EditorWindow
//     {
//         private GameObject innerCenterIndicatorPrefab;
//         private GameObject outerCenterIndicatorPrefab;
//         private GameObject cityGameObject;
//         private CityAuthoring cityAuthoring;
//
//         [MenuItem("Tools/Grid Indicator Window")]
//         public static void ShowWindow()
//         {
//             GetWindow<GridIndicatorWindow>("Grid Indicator");
//         }
//
//         private void OnGUI()
//         {
//             GUILayout.Label("Grid Indicator Settings", EditorStyles.boldLabel);
//
//             // Prefab拖拽区域
//             innerCenterIndicatorPrefab = (GameObject)EditorGUILayout.ObjectField("Inner Center Indicator Prefab",
//                 innerCenterIndicatorPrefab, typeof(GameObject), false);
//
//             outerCenterIndicatorPrefab = (GameObject)EditorGUILayout.ObjectField("Outer Center Indicator Prefab",
//                 outerCenterIndicatorPrefab, typeof(GameObject), false);
//
//             // City GameObject拖拽区域
//             cityGameObject = (GameObject)EditorGUILayout.ObjectField("City GameObject",
//                 cityGameObject, typeof(GameObject), true);
//
//             EditorGUILayout.Space();
//
//             if (GUILayout.Button("Generate Indicators"))
//             {
//                 GenerateIndicators();
//             }
//
//             if (GUILayout.Button("Clear All Indicators"))
//             {
//                 ClearAllIndicators();
//             }
//         }
//
//         private void GenerateIndicators()
//         {
//             if (innerCenterIndicatorPrefab == null || outerCenterIndicatorPrefab == null || cityGameObject == null)
//             {
//                 EditorUtility.DisplayDialog("Error", "Please assign all required fields!", "OK");
//                 return;
//             }
//
//             cityAuthoring = cityGameObject.GetComponent<CityAuthoring>();
//             if (cityAuthoring == null)
//             {
//                 EditorUtility.DisplayDialog("Error", "City GameObject doesn't have CityAuthoring component!", "OK");
//                 return;
//             }
//
//             if (cityAuthoring.nineGridInfos == null || cityAuthoring.nineGridInfos.Count == 0)
//             {
//                 EditorUtility.DisplayDialog("Info", "No nineGridInfos data found!", "OK");
//                 return;
//             }
//
//             // 创建父对象来组织生成的指示器
//             GameObject parentObject = new GameObject($"GridIndicators_City_{cityAuthoring.globalIdx}");
//
//             for (int i = 0; i < cityAuthoring.nineGridInfos.Count; i++)
//             {
//                 var gridInfo = cityAuthoring.nineGridInfos[i];
//                 CreateIndicatorForGridInfo(gridInfo, i, parentObject.transform);
//             }
//
//             EditorUtility.DisplayDialog("Success", $"Generated {cityAuthoring.nineGridInfos.Count} grid indicators!",
//                 "OK");
//         }
//
//         private void CreateIndicatorForGridInfo(LoadingGridInfo gridInfo, int index, Transform parent)
//         {
//             // 实例化Outer Center Indicator
//             if (outerCenterIndicatorPrefab != null)
//             {
//                 GameObject outerIndicator = (GameObject)PrefabUtility.InstantiatePrefab(outerCenterIndicatorPrefab);
//                 outerIndicator.transform.position = gridInfo.outerCenter;
//                 outerIndicator.name = $"OuterIndicator_{index}";
//                 outerIndicator.transform.SetParent(parent);
//
//                 // 如果有size信息，可以设置缩放
//                 if (gridInfo.outerSize > 0)
//                 {
//                     outerIndicator.transform.localScale = Vector3.one * gridInfo.outerSize;
//                 }
//             }
//
//             // 实例化Inner Center Indicator
//             if (innerCenterIndicatorPrefab != null)
//             {
//                 GameObject innerIndicator = (GameObject)PrefabUtility.InstantiatePrefab(innerCenterIndicatorPrefab);
//                 innerIndicator.transform.position = gridInfo.innerCenter;
//                 innerIndicator.name = $"InnerIndicator_{index}";
//                 innerIndicator.transform.SetParent(parent);
//
//                 // 如果有size信息，可以设置缩放
//                 if (gridInfo.innerSize > 0)
//                 {
//                     innerIndicator.transform.localScale = Vector3.one * gridInfo.innerSize;
//                 }
//             }
//         }
//
//         private void ClearAllIndicators()
//         {
//             GameObject[] indicators = GameObject.FindObjectsOfType<GameObject>();
//             List<GameObject> toDelete = new List<GameObject>();
//
//             foreach (GameObject go in indicators)
//             {
//                 if (go.name.StartsWith("GridIndicators_City_") ||
//                     go.name.StartsWith("OuterIndicator_") ||
//                     go.name.StartsWith("InnerIndicator_"))
//                 {
//                     toDelete.Add(go);
//                 }
//             }
//
//             foreach (GameObject go in toDelete)
//             {
//                 DestroyImmediate(go);
//             }
//
//             if (toDelete.Count > 0)
//             {
//                 EditorUtility.DisplayDialog("Success", $"Cleared {toDelete.Count} indicators!", "OK");
//             }
//             else
//             {
//                 EditorUtility.DisplayDialog("Info", "No indicators found to clear!", "OK");
//             }
//         }
//     }
// }