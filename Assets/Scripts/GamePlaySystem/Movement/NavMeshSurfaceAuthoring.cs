using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;


namespace SparFlame.GamePlaySystem.Movement
{
    public class NavMeshSurfaceAuthoring : MonoBehaviour
    {
        public NavMeshData data;
        public class Baker : Baker<NavMeshSurfaceAuthoring>
        {
            public override void Bake(NavMeshSurfaceAuthoring authoring) 
            {
                // var surface = GetComponent<NavMeshSurface>();
                // AddComponentObject(new NavSurfaceSpawn { Surface = surface });
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new NavMeshDataComponent
                {
                    Data = authoring.data,
                });
                
                // AddComponentObject(entity, authoring.surface.navMeshData);
                
            }
        }
    }

    public class NavMeshDataComponent : IComponentData
    {
        public NavMeshData Data;
    }
}