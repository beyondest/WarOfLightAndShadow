using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SparFlame.Utils
{
        public class LoadingProgress : IProgress<float>
        {
            public event Action<float> ProgressChanged;
            private readonly float _denominator;
            public LoadingProgress(float denominator = 1f)
            {
                this._denominator = denominator;
            }
            
            public void Report(float value)
            {
                ProgressChanged?.Invoke(value / _denominator);
            }
        }
        
        
        public readonly struct AsyncOperationGroup
        {
            public readonly List<AsyncOperation> Operations;
            /// <summary>
            /// This is the average progress of all operations in the group
            /// </summary>
            public float AverageProgress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
            public bool IsDone => Operations.All(o => o.isDone);
            public AsyncOperationGroup(int initialOperationCount = 1)
            {
                Operations = new List<AsyncOperation>(initialOperationCount);
            }

            public void Add(AsyncOperation operation)
            {
                Operations.Add(operation);
            }
        }
        
        public class AddressableResourceGroup
        {
            private readonly List<AsyncOperationHandle> _handles;
            
            public float AverageProgress => _handles.Count == 0 ? 0 : _handles.Average(o => o.PercentComplete);
    
            public bool IsDone => _handles.All(o => o.IsDone);

            public bool IsHandleCreated(int targetCount)
            {
                return targetCount >= _handles.Count;
            }


            public AddressableResourceGroup(int initialHandlesCount = 1)
            {
                _handles = new List<AsyncOperationHandle>(initialHandlesCount);
            }
            public void Add(AsyncOperationHandle handle)
            {
                _handles.Add(handle);
            }

            public void Release()
            {
                foreach (var operation in _handles)
                {
                    Addressables.Release(operation);
                }
                _handles.Clear();
            }
        }

        
        
        
}