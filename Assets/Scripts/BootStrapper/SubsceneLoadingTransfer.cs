using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine.SceneManagement;

namespace SparFlame.BootStrapper
{
    // [RequireMatchingQueriesForUpdate]
    public partial class SubsceneLoadingTransfer : SystemBase
    {
        private EntityQuery _sceneSections;
        private bool _isLoading;

        protected override void OnCreate()
        {
            _sceneSections = SystemAPI.QueryBuilder().WithAll<SceneSectionData>().WithAll<RequestSceneLoaded>().Build();
        }

        protected override void OnStartRunning()
        {
            // if (!SceneController.Instance) return;
            SceneController.Instance.EcsStartLoadScene += () => _isLoading = true;
        }

        protected override void OnUpdate()
        {
            if (!_isLoading) return;
            var entities = _sceneSections.ToEntityArray(WorldUpdateAllocator);
            var loadedSceneSections = 0f;
            foreach (var entity in entities)
            {
                if (SceneSystem.IsSectionLoaded(World.Unmanaged, entity))
                {
                    loadedSceneSections++;
                }
            }

            var loadProgress = 0f;
            if (entities.Length != 0)
                loadProgress = loadedSceneSections / entities.Length;
            SceneController.Instance.SetSubsceneLoadingProgress(loadProgress);
            entities.Dispose(Dependency);
            if (loadProgress > 0.99f)
                _isLoading = false;
        }
    }
}