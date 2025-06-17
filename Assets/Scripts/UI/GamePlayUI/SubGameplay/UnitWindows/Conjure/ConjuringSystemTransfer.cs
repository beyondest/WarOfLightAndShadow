using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.Hints;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.UI.SubGameplay
{
    public partial class ConjuringSystemTransfer : SystemBase
    {
        private InputConjureData _inputConjureData;
        private bool _initEvents;
        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
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
                if (MiniConjureWindow.Instance.TryGetConjureInfo(_inputConjureData.HotKeyIndex, out var index))
                {
                    MiniConjureWindow.Instance.OnClickSlot(index);
                    // if (maxConjureCount == 0)
                    // {
                    //     var hintRequest = EntityManager.CreateEntity();
                    //     EntityManager.AddComponent<HintRequest>(hintRequest);
                    //     EntityManager.SetComponentData(hintRequest, new HintRequest
                    //     {
                    //         Name = HintName.NotEnoughResource,
                    //     });
                    //     return;
                    // }
                    // ConjureUnits(unit,  1, building,maxConjureCount);
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
            ecb.AddComponent<SubGameplayEntityTag>(conjureRequest);
            
            var costList = SystemAPI.GetBuffer<CostList>(conjureUnit);
            foreach (var cost in costList)
            {
                var costRequest = ecb.CreateEntity();
                ecb.AddComponent(costRequest, new ResourceChangeRequest
                {
                    AbsAmount = math.abs(cost.Amount * actualConjureCount),
                    FromFaction = SystemAPI.GetComponent<SubGameplayGeneralAttr>(buildingEntity).FactionTag,
                    Type = cost.Type,
                    RequestType = ResourceRequestType.Consume
                });
                ecb.AddComponent<SubGameplayEntityTag>(conjureRequest);

            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}