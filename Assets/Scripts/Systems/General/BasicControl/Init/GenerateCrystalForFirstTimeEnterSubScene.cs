using SparFlame.Components.General;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.General.BasicControl.Init
{
    internal struct OnlyRunOnceForCrystalGeneration : IComponentData
    {
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GenerateCrystalForFirstTimeEnterSubScene : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<CrystalPrefab>();
            state.RequireForUpdate<GenerateCrystalRequest>();
            state.RequireForUpdate<OnlyRunOnceForCrystalGeneration>();
            state.RequireForUpdate<EnableDebugInitSceneGroup>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var request = SystemAPI.GetSingleton<GenerateCrystalRequest>();
            var crystalPrefab = SystemAPI.GetSingleton<CrystalPrefab>();
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
            var crystalEntity = state.EntityManager.Instantiate(playerFaction == FactionTag.Light
                ? crystalPrefab.LightCrystalPrefab
                : crystalPrefab.DarkCrystalPrefab);
            state.EntityManager.AddComponent<SubGameplayEntityTag>(crystalEntity);
            var trans = SystemAPI.GetComponent<LocalTransform>(crystalEntity);
            trans.Position = request.Position;
            SystemAPI.SetComponent(crystalEntity, trans);
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<GenerateCrystalRequest>());
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<OnlyRunOnceForCrystalGeneration>());
            var enableDebugInitSceneGroup = SystemAPI.GetSingleton<EnableDebugInitSceneGroup>();
            if (enableDebugInitSceneGroup.value)
                SceneController.Instance.LoadDebugInitSubscene();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}