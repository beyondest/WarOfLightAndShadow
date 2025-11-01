using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Editor
{
    public class SplitItemsFromSoEditor : EditorWindow
    {
        private ScriptableObject _sourceSo;
        int countToSplit = 1;

        [MenuItem("Tools/Database/Split SO Items")]
        public static void Open()
        {
            GetWindow<SplitItemsFromSoEditor>("Split SO Items");
        }
        private void OnGUI()
        {
            _sourceSo = (ScriptableObject)EditorGUILayout.ObjectField("Source SO", _sourceSo, typeof(ScriptableObject),
                false);
            countToSplit = EditorGUILayout.IntField("Count to Split", countToSplit);

            if (GUILayout.Button("Split"))
            {
                if (_sourceSo)
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
            var itemsField = _sourceSo.GetType()
                .GetField("items", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (itemsField == null)
            {
                Debug.LogError("No 'items' field found in the selected ScriptableObject.");
                return;
            }

            var itemsValue = itemsField.GetValue(_sourceSo) as IList;
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

            var newSo = ScriptableObject.CreateInstance(_sourceSo.GetType());

            var newItemsList = (IList)System.Activator.CreateInstance(itemsValue.GetType());
            foreach (var item in newItems)
            {
                newItemsList.Add(item);
            }

            itemsField.SetValue(newSo, newItemsList);

            string sourcePath = AssetDatabase.GetAssetPath(_sourceSo);
            string sourceDir = System.IO.Path.GetDirectoryName(sourcePath);
            string newSoPath = AssetDatabase.GenerateUniqueAssetPath(sourceDir + "/" + _sourceSo.name + "_Split.asset");

            AssetDatabase.CreateAsset(newSo, newSoPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.SetDirty(_sourceSo);

            Debug.Log($"Split {countToSplit} items into new SO: {newSoPath}");
        }
    }
}