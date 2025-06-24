using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.VFX;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.MainGameplay
{
    public struct ArmyGroupMulti2DRealTimeInfo
    {
        public ArmyGroupIconType IconType;
        public Entity Entity;
    }
    public partial class ArmyGroupMulti2DWindowSystem : SystemBase
    {
         private NativeList<ArmyGroupMulti2DRealTimeInfo> _infos;
        private bool _initEvents;

        protected override void OnCreate()
        {
            RequireForUpdate<MainGamingTag>();
            _infos = new NativeList<ArmyGroupMulti2DRealTimeInfo>(Allocator.Persistent);
        }

        protected override void OnStartRunning()
        {
            if (!_initEvents)
            {
                _initEvents = true;
                ArmyGroupMulti2DWindow.Instance.GetTargetEntityByIndex += index =>
                {
                    var data = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
                    UpdateSelectedInfos(data);
                    var targetEntity = index < _infos.Length ? _infos[index].Entity : Entity.Null;
                    ArmyGroupMulti2DWindow.Instance.GetSelectionData(_infos.Length, targetEntity
                    );
                    ArmyGroupMulti2DWindow.Instance.DeselectAllExceptOne += DeselectAllExceptOne;
                };
            }
        }

        protected override void OnUpdate()
        {
            var selectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            if (!_initEvents) return;
            if (!ArmyGroupMulti2DWindow.Instance.IsOpened()) return;
            UpdateSelectedInfos(selectionData);
        }

        private void UpdateSelectedInfos(ArmyGroupSelectionData selectionData)
        {
            _infos.Clear();
            foreach (var (armyGroupAttr, entity) in SystemAPI
                         .Query<RefRO<ArmyGroupAttr>>()
                         .WithEntityAccess().WithAll<ArmyGroupSelected>())
            {
                _infos.Add(new ArmyGroupMulti2DRealTimeInfo
                {
                    Entity = entity,
                    IconType = armyGroupAttr.ValueRO.IconType
                });
            }
            ArmyGroupMulti2DWindow.Instance.UpdateSelectedView(_infos, selectionData.CurrentSelectFaction);
        }
        private void DeselectAllExceptOne(Entity targetEntity)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_,entity) in SystemAPI.Query<RefRO<ArmyGroupSelected>>().WithEntityAccess())
            {
                if(entity == targetEntity)continue;
                ecb.SetComponentEnabled<ArmyGroupSelected>(entity,false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent<MainGameplayEntityTag>(vfxRequest);
                ecb.AddComponent(vfxRequest,new VFXRequest
                {
                    VFXName = VFXName.ArmyGroupSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = entity
                });
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


        protected override void OnDestroy()
        {
            if (_infos.IsCreated)
                _infos.Dispose();
        }
    }
}