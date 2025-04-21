using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Spawn;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public partial class ConjuringSystemTransfer : SystemBase
    {
        private InputConjureData _inputConjureData;
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputConjureData>();
            RequireForUpdate<ConjureSystemConfig>();
        }
        

        protected override void OnUpdate()
        {
            
            if(ConjureWindow.Instance == null || MiniConjureWindow.Instance == null) return;
            if (!ConjureWindow.Instance.InitWindowEvents)
            {
                ConjureWindow.Instance.InitWindowEvents = true;
                ConjureWindow.Instance.EcsConjureUnits += ConjureUnits;
            }
            if (!MiniConjureWindow.Instance.InitWindowEvents)
            {
                MiniConjureWindow.Instance.InitWindowEvents = true;
                MiniConjureWindow.Instance.EcsConjureUnit += (unit, building,maxConjureCount) =>
                {
                    ConjureUnits(unit, 1,building, maxConjureCount);
                };
            }
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
            
            var costList = SystemAPI.GetBuffer<CostList>(conjureUnit);
            foreach (var cost in costList)
            {
                var costRequest = ecb.CreateEntity();
                ecb.AddComponent(costRequest, new ResourceChangeRequest
                {
                    Amount = -cost.Amount * actualConjureCount,
                    FromFaction = SystemAPI.GetComponent<GeneralAttr>(buildingEntity).FactionTag,
                    Type = cost.Type,
                    RequestType = ResourceRequestType.Consume
                });
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}