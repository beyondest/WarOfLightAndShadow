using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Conjure;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.UI.GamePlay
{
    public partial class ConjuringSystemTransfer : SystemBase
    {
        private InputConjureData _inputConjureData;
        private bool _initEvents;
        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<InputConjureData>();
            RequireForUpdate<ConjureSystemConfig>();
        }

        protected override void OnStartRunning()
        {
            if (!_initEvents)
            {
                _initEvents = true;
                ConjureWindow.Instance.EcsConjureUnits += ConjureUnits;
                MiniConjureWindow.Instance.EcsConjureUnit += (unit, building,maxConjureCount) =>
                {
                    ConjureUnits(unit, 1,building, maxConjureCount);
                };
            }
        }

        protected override void OnUpdate()
        {
            if(!MiniConjureWindow.Instance.IsOpened())return;
            _inputConjureData = SystemAPI.GetSingleton<InputConjureData>();
            CheckHotkeyConjure();
        }

        private void CheckHotkeyConjure()
        {
            if (_inputConjureData.HotKeyIndex > 0)
            {
                if (MiniConjureWindow.Instance.TryGetConjureInfo(_inputConjureData.HotKeyIndex, out var unit,
                        out var building,out var maxConjureCount))
                {
                    ConjureUnits(unit,  1, building,maxConjureCount);
                }
            }
        }

        private void ConjureUnits(Entity conjureUnit, int count, Entity buildingEntity,int maxConjureCount)
        {
            var actualConjureCount = _inputConjureData.FullConjure ? maxConjureCount : count;
            if(actualConjureCount < 0)return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var conjureRequest = ecb.CreateEntity();
            ecb.AddComponent(conjureRequest, new ConjureRequest
            {
                BuildingEntity = buildingEntity,
                Count = actualConjureCount,
                UnitPrefab = conjureUnit
            });
            ecb.AddComponent<GameplayEntityTag>(conjureRequest);
            
            var costList = SystemAPI.GetBuffer<CostList>(conjureUnit);
            foreach (var cost in costList)
            {
                var costRequest = ecb.CreateEntity();
                ecb.AddComponent(costRequest, new ResourceChangeRequest
                {
                    AbsAmount = math.abs(cost.Amount * actualConjureCount),
                    FromFaction = SystemAPI.GetComponent<GeneralAttr>(buildingEntity).FactionTag,
                    Type = cost.Type,
                    RequestType = ResourceRequestType.Consume
                });
                ecb.AddComponent<GameplayEntityTag>(conjureRequest);

            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}