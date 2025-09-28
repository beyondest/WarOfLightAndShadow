// using System;
// using Unity.Entities;
// using UnityEngine;
//
//
// namespace SparFlame.Test
// {
//     public struct ConditionalTestSavingComponentA : IComponentData
//     {
//         public int Value;
//     }
//
//     public struct ConditionalTestSavingComponentB : IComponentData
//     {
//         public float Value;
//     }
//
//     public struct ConditionalTestSavingBuffer : IBufferElementData
//     {
//         public float Value;
//     }
//
//     public struct ConditionalTestSavingBuffer2 : IBufferElementData
//     {
//         public float Value;
//     }
//
//     public struct TestSavingComponentA : IComponentData
//     {
//         public int Value;
//     }
//
//     public struct TestSavingComponentB : IComponentData
//     {
//         public float Value;
//     }
//
//     public struct TestSavingBuffer : IBufferElementData
//     {
//         public float Value;
//     }
//
//     [Serializable]
//     public struct TestSavingBuffer2 : IBufferElementData
//     {
//         public float value;
//     }
//     
//     [CreateAssetMenu(fileName = "TestSO", menuName = "DomainReload/TestSO")]
//     public class TestSO : ScriptableObject
//     {
//         public int soValue = 100;
//         [NonSerialized]public int testvalue = 999;
//     }
// }