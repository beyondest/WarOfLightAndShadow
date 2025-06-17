using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.BootStrapper
{
    
    public partial class AudioTransfer : SystemBase
    {

        private EntityQuery _audioRequestQuery;
        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
            RequireForUpdate<AudioTransferConfig>();
            _audioRequestQuery = SystemAPI.QueryBuilder().WithAll<AudioRequest>().Build();
        }
 

        protected override void OnUpdate()
        {
            if(_audioRequestQuery.IsEmpty)return;
            var config = SystemAPI.GetSingleton<AudioTransferConfig>();
            var cameraData = SystemAPI.GetSingleton<CameraData>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var audioRequestEntities = _audioRequestQuery.ToEntityArray(Allocator.Temp);
            var requests = _audioRequestQuery.ToComponentDataArray<AudioRequest>(Allocator.Temp);
            for(var i = 0; i < audioRequestEntities.Length; i++)
            {
                var entity = audioRequestEntities[i];
                var request = requests[i];
                ecb.DestroyEntity(entity);
                var dis = math.distancesq(request.Position, cameraData.CameraRigPosition);

                if (config.AudioRadiusSqToCameraRig <=dis)continue;
                AudioManager.Instance.PlayAtPosition(request.Name, request.Position);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
            audioRequestEntities.Dispose();
            requests.Dispose();
        }
    }


    public enum AudioName
    {
        None = 0,
        ErrorAction = 1,
        LightShield = 2,
        DarkShield = 3,
        Spear = 4,
        LightDead = 5,
        DarkDead = 6,
        LightFlameBurn = 7,
        DarkFlameBurn = 8,
        MagicSwordSplash = 9,
        ClericHealCircle = 10,
        ArrowShoot = 11,
        TowerMagicBallStart = 12,
        TowerMagicCircle = 13,
        TowerMagicBallHit = 14,
        BuildingDestroyed = 15,
        WorkerHarvest = 16,
        Construct = 17,
        NextTier = 18,
        Recycle = 19,
        UnitUpgrade = 20,
        ArmyGroupStartMoving = 21,
    }
    
    public struct AudioRequest : IComponentData
    {
        public AudioName Name;
        public float3 Position;
    }
}