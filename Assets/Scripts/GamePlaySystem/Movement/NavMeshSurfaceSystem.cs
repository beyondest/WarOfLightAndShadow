using Unity.Entities;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace SparFlame.GamePlaySystem.Movement
{
    // [CreateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(
        typeof(InitializationSystemGroup))] 
    public partial struct NavSurfaceSpawnSystem : ISystem
    {
        // private EndSimulationEntityCommandBufferSystem.Singleton _ecbCreator;
        private int _count;

        public void OnCreate(ref SystemState state)
        {
            // state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            // _ecbCreator = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if(_count > 0)return;

            // var ecb = _ecbCreator.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (surface, e) in SystemAPI.Query<NavMeshDataComponent>().WithEntityAccess())
            {
                _count++;
                //Debug.Log("Try Add Navmesh Data");
                NavMesh.AddNavMeshData(surface.Data);
                // var ecb = _ecbCreator.CreateCommandBuffer(state.WorldUnmanaged);
                // foreach(var (surface, e) in SystemAPI.Query<NavSurfaceSpawn>().WithEntityAccess()) {
                //     GameObject.Instantiate(surface.Surface); //create the gameobject version of the navmeshsurface
                //     ecb.DestroyEntity(e); //defer destroying because it is a structural change
                // }
            }
        }
    }
}