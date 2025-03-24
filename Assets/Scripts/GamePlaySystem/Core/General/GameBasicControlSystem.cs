using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;

namespace SparFlame.GamePlaySystem.General
{
    /// <summary>
    /// The only system that should not rely on anything, and keep running all the time
    /// This system deals with all pause/resume request, and make that request truly work
    /// SimulationSystems all run after monobehavior update
    /// </summary>
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial class GameBasicControlSystem : SystemBase
    {
        private bool _isPaused;

        protected override void OnUpdate()
        {
            var pauseQuery = SystemAPI.QueryBuilder().WithAll<PauseRequest>().Build();
            var resumeQuery = SystemAPI.QueryBuilder().WithAll<ResumeRequest>().Build();
            
            if ( pauseQuery.CalculateEntityCount() != 0)
            {
                var pauseEntities = pauseQuery.ToEntityArray(Allocator.Temp);
                foreach (var entity in pauseEntities)
                    EntityManager.DestroyEntity(entity);
                PauseGame(true);
            }

            if (resumeQuery.CalculateEntityCount() == 0) return;
            var resumeEntities = resumeQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in resumeEntities)
                EntityManager.DestroyEntity(entity);
            PauseGame(false);
        }

        private void PauseGame(bool isPausing)
        {
            if (_isPaused == isPausing) return;
            if (isPausing)
            {
                UnityEngine.Time.timeScale = 0;
                var entityQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] { typeof(NotPauseTag) },
                });
                if (!entityQuery.TryGetSingletonEntity<NotPauseTag>(out var entity)) return;
                EntityManager.SetEnabled(entity, false);
                _isPaused = true;
            }
            else
            {
                UnityEngine.Time.timeScale = 1;
                var entityQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
                 {
                     All = new ComponentType[] { typeof(NotPauseTag) },
                     Options = EntityQueryOptions.IncludeDisabledEntities 
                 });
                 if (entityQuery.TryGetSingletonEntity<NotPauseTag>(out var entity))
                 {
                     EntityManager.SetEnabled(entity, true);
                 }
                _isPaused = false;
            }
        }
    }
}