using System;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Core.Structs
{
    [Serializable]
    public struct Range
    {
        public float lower;
        public float upper;
    }

    public struct IntPair : IEquatable<IntPair>
    {
        public int A;
        public int B;

        public IntPair(int a, int b)
        {
            if (a < b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public bool Equals(IntPair other) => A == other.A && B == other.B;
        public override int GetHashCode() => (A * 73856093) ^ (B * 19349663);
    }

    public struct QueriesGroup: IDisposable
    {
        private NativeList<EntityQuery> _queries;

        public QueriesGroup(Allocator allocator)
        {
            _queries = new NativeList<EntityQuery>(allocator);
        }
        public bool IsAllNotEmpty()
        {
            foreach (var query in _queries)
            {
                if(query.IsEmpty)return false;
            }
            return true;
        }

        public bool AllIsEmpty()
        {
            foreach (var query in _queries)
            {
                if(!query.IsEmpty)return false;
            }
            return true;
        }

        public void AddQuery(EntityQuery query)
        {
            _queries.Add(query);
        }

        public void ClearQueries()
        {
            foreach (var query in _queries)
            {
                query.Dispose();
            }
            _queries.Clear();
        }

        public void Dispose()
        {
            ClearQueries();
            _queries.Dispose();
        }
    }
    
}