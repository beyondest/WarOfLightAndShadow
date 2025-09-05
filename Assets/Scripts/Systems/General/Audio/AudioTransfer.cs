using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.General.Audio
{
    
    public partial class AudioTransfer : SystemBase
    {

        private EntityQuery _audioRequestQuery;
        protected override void OnCreate()
        {
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


}