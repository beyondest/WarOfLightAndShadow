using UnityEngine;
using Unity.Collections;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace SparFlame.Test
{
  
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshNodeEdgesDrawer : MonoBehaviour
    {
        public NavMeshAgent agent;
        public Vector3 extents = Vector3.one;
        void Update()
        {
            var query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Temp);
            var vertices = new NativeArray<Vector3>(6, Allocator.Temp);
            var neighbors = new NativeArray<PolygonId>(10, Allocator.Temp);
            var edgeIndices = new NativeArray<byte>(neighbors.Length, Allocator.Temp);
            int totalVertices;
            int totalNeighbors;

            var location = query.MapLocation(transform.position, extents, agent.agentTypeID);

            var queryStatus = query.GetEdgesAndNeighbors(
                location.polygon, vertices, neighbors, edgeIndices,
                out totalVertices, out totalNeighbors);

            var color = (queryStatus & PathQueryStatus.Success) != 0 ? Color.green : Color.red;
            Debug.DrawLine(transform.position - Vector3.up, transform.position + Vector3.up, color);

            for (int i = 0, j = totalVertices - 1; i < totalVertices; j = i++)
            {
                Debug.DrawLine(vertices[i], vertices[j], Color.grey);
            }

            for (var i = 0; i < totalNeighbors; i++)
            {
                if (query.GetPolygonType(neighbors[i]) == NavMeshPolyTypes.OffMeshConnection)
                {
                    // The link neighbor is not connected through any of the polygon's edges.
                    // Call GetEdgesAndNeighbors() on this specific neighbor in order to retrieve its edges.
                    continue;
                }

                var start = edgeIndices[i];
                var end = (start + 1) % totalVertices;
                var neighborColor = Color.Lerp(Color.yellow, Color.magenta, 1f * start / (totalVertices - 1));
                Debug.DrawLine(vertices[start], vertices[end], neighborColor);
            }

            query.Dispose();
            vertices.Dispose();
            neighbors.Dispose();
            edgeIndices.Dispose();
        }
    }
}