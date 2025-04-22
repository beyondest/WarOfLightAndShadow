using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map
{
    public struct MapUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideGrid(in float3 gridPos,in float3 gridSize,in float3 curPos)
        {
            var xMax = gridPos.x + gridSize.x / 2;
            var zMax = gridPos.z + gridSize.z / 2;
            var xMin = gridPos.x - gridSize.x / 2;
            var yMin = gridPos.z - gridSize.z / 2;
            if (curPos.x < xMin || curPos.x > xMax
                                  || curPos.z < yMin || curPos.z > zMax)
                return false;
            return true;
        }
    }
}