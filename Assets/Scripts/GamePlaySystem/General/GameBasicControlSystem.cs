using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.General
{
    /// <summary>
    /// The only system that should not rely on anything, and keep running all the time
    /// This system deals with all pause/resume request, and make that request truly work
    /// </summary>
    public partial class GameBasicControlSystem : SystemBase
    {
        private bool _isPaused = false;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

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
                var pauseEntity = SystemAPI.GetSingletonEntity<NotPauseTag>();
                EntityManager.SetEnabled(pauseEntity, false);
                _isPaused = true;
            }
            else
            {
                UnityEngine.Time.timeScale = 1;
                var e = EntityManager.CreateEntityQuery(new EntityQueryDesc
                 {
                     All = new ComponentType[] { typeof(NotPauseTag) },
                     Options = EntityQueryOptions.IncludeDisabledEntities // 允许查找被禁用的组件
                 });
                 if (e.TryGetSingletonEntity<NotPauseTag>(out var entity))
                 {
                     EntityManager.SetEnabled(entity, true);
                 }
                _isPaused = false;
            }
        }
    }
}