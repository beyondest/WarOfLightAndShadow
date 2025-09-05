using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Editor
{
    public class SplitItemsFromSOEditor : EditorWindow
    {
        ScriptableObject sourceSO;
        int countToSplit = 1;

        [MenuItem("Tools/Database/Split SO Items")]
        public static void Open()
        {
            GetWindow<SplitItemsFromSOEditor>("Split SO Items");
        }
        private void OnGUI()
        {
            sourceSO = (ScriptableObject)EditorGUILayout.ObjectField("Source SO", sourceSO, typeof(ScriptableObject),
                false);
            countToSplit = EditorGUILayout.IntField("Count to Split", countToSplit);

            if (GUILayout.Button("Split"))
            {
                if (sourceSO != null)
                {
                    SplitItems();
                }
                else
                {
                    Debug.LogWarning("Please select a source ScriptableObject first!");
                }
            }
        }

        private void SplitItems()
        {
            // 通过反射找到 items 字段
            var itemsField = sourceSO.GetType()
                .GetField("items", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (itemsField == null)
            {
                Debug.LogError("No 'items' field found in the selected ScriptableObject.");
                return;
            }

            var itemsValue = itemsField.GetValue(sourceSO) as IList;
            if (itemsValue == null)
            {
                Debug.LogError("'items' field is not a List or is null.");
                return;
            }

            int originalCount = itemsValue.Count;
            if (countToSplit <= 0 || countToSplit > originalCount)
            {
                Debug.LogError("Invalid split count.");
                return;
            }

            var newItems = new List<object>();
            for (int i = originalCount - countToSplit; i < originalCount; i++)
            {
                newItems.Add(itemsValue[i]);
            }

            for (int i = 0; i < countToSplit; i++)
            {
                itemsValue.RemoveAt(itemsValue.Count - 1); // always remove from end
            }

            var newSO = ScriptableObject.CreateInstance(sourceSO.GetType());

            var newItemsList = (IList)System.Activator.CreateInstance(itemsValue.GetType());
            foreach (var item in newItems)
            {
                newItemsList.Add(item);
            }

            itemsField.SetValue(newSO, newItemsList);

            string sourcePath = AssetDatabase.GetAssetPath(sourceSO);
            string sourceDir = System.IO.Path.GetDirectoryName(sourcePath);
            string newSOPath = AssetDatabase.GenerateUniqueAssetPath(sourceDir + "/" + sourceSO.name + "_Split.asset");

            AssetDatabase.CreateAsset(newSO, newSOPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.SetDirty(sourceSO);

            Debug.Log($"Split {countToSplit} items into new SO: {newSOPath}");
        }
    }
}