using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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
            public AsyncOperationGroup(int initialOperationCount)
            {
                Operations = new List<AsyncOperation>(initialOperationCount);
            }

            public void Add(AsyncOperation operation)
            {
                Operations.Add(operation);
            }
        }
        
        
}