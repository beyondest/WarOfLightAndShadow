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

    public readonly struct IntPair : IEquatable<IntPair>
    {
        private readonly int _a;
        private readonly int _b;

        public IntPair(int a, int b)
        {
            if (a < b)
            {
                _a = a;
                _b = b;
            }
            else
            {
                _a = b;
                _b = a;
            }
        }

        public bool Equals(IntPair other) => _a == other._a && _b == other._b;
        public override int GetHashCode() => (_a * 73856093) ^ (_b * 19349663);
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