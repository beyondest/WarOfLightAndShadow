// using UnityEditor;
// using UnityEngine.UIElements;
// using System;
// using SparFlame.GamePlaySystem.Building;
//
// namespace Editor
// {
//
//     [CustomPropertyDrawer(typeof(BuildingData), true)]
//     public class BuildingDataDrawer : PropertyDrawer
//     {
//         public override VisualElement CreatePropertyGUI(SerializedProperty property)
//         {
//             var container = new VisualElement();
//             var typeField = new EnumField("Building Type", (BuildingType)property.FindPropertyRelative("BuildingType").enumValueIndex);
//             container.Add(typeField);
//
//             typeField.RegisterValueChangedCallback(evt =>
//             {
//                 ChangeBuildingType(property, evt.newValue);
//             });
//
//             return container;
//         }
//
//         private void ChangeBuildingType(SerializedProperty property, Enum newType)
//         {
//             BuildingType selectedType = (BuildingType)newType;
//             Type targetClass = typeof(BuildingData);
//
//             switch (selectedType)
//             {
//                 case BuildingType.Fortifications:
//                     targetClass = typeof(FortificationData);
//                     break;
//                 case BuildingType.Ornaments:
//                     targetClass = typeof(OrnamentData);
//                     break;
//             }
//
//             if (property.managedReferenceValue?.GetType() != targetClass)
//             {
//                 property.managedReferenceValue = Activator.CreateInstance(targetClass);
//                 property.serializedObject.ApplyModifiedProperties();
//             }
//         }
//     }
//
// }