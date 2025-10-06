using Unity.Collections;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestSequece : MonoBehaviour
    {
        void Start()
        {
            var map = new NativeParallelMultiHashMap<int, int>(10, Allocator.Temp);

            // 对同一个 key 插入多个值
            map.Add(1, 100);
            map.Add(1, 200);
            map.Add(1, 300);

            // 遍历两次，看看顺序是否一致
            Debug.Log("First iteration:");
            var iter = map.GetValuesForKey(1);
            foreach (var v in iter)
            {
                Debug.Log(v);
            }

            Debug.Log("Second iteration:");
            iter = map.GetValuesForKey(1);
            foreach (var v in iter)
            {
                Debug.Log(v);
            }

            map.Dispose();
        }
    }
}