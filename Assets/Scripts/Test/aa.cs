using System;
using Unity.Entities;
using UnityEditor;

namespace SparFlame.Test
{
    public partial class aa : SystemBase
    {
        protected override void OnUpdate()
        {
            
        }
    }

    public struct Test1
    {
        
    }
    public struct Test2
    {
        
    }
    
    public struct TestStruct
    {
        private void Test(Test1 t1)
        {
            var f  = TestFlag.Flag1;
            var a = f.HasFlag(TestFlag.Flag1);
        }

        private void Test(Test2 t2)
        {
            
        }
    }

    [CustomPropertyDrawer(typeof(TestStruct))]
    public class TestDrawer : PropertyDrawer
    {
        
    }
}

[Flags]
public enum TestFlag
{
    Flag1,
    Flag2,
}