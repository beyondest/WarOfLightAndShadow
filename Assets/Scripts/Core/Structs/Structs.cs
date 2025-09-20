using System;
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
}