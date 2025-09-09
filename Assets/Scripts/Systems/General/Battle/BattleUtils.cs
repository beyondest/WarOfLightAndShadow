using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.General.Battle
{
    public static class BattleUtils
    {
        public static void BeginBattle(SubGameStatus targetSubGameStatus, 
            Entity attacker,
            Entity defender,
            int index,
            EntityCommandBuffer.ParallelWriter ecb)
        {
            var warRequest = ecb.CreateEntity(index);
            ecb.AddComponent<MainGameplayEntityTag>(index, warRequest);
            
            ecb.AddComponent(index, warRequest, new BattleTriggerRequest
            {
                Attacker = attacker,
                Defender = defender,
                TargetSubGameStatus = targetSubGameStatus
            });
        }
        
        public static NativeList<int> GetClosestAvailableGrids(
            float3 playerPos,
            in LocalTransform cityTransform,
            DynamicBuffer<CityAvailableGridNumber> availableGrids,
            Allocator allocator)
        {
            var result = new NativeList<int>(allocator);
            var cityPos = cityTransform.Position;
            var cityRot = cityTransform.Rotation;

            // 1. 方向向量（归一化，忽略Y）
            var dir = math.normalize(new float3(playerPos.x - cityPos.x, 0, playerPos.z - cityPos.z));

            // 2. 转换到城市局部空间
            var localDir = math.rotate(math.inverse(cityRot), dir);

            // 3. 计算角度 (atan2: Z向前, X向右)
            var angle = math.degrees(math.atan2(localDir.x, localDir.z));
            if (angle < 0) angle += 360f;

            // 4. 映射到格子编号（0–7）
            // 0: 西北, 1: 北, 2: 东北, 3: 东, 4: 东南, 5: 南, 6: 西南, 7: 西
            var mainDir = (int)math.round(angle / 45f) % 8;

            // 5. 最近的三个格子编号
            int[] candidates = {
                mainDir,
                (mainDir + 7) % 8, // 左邻
                (mainDir + 1) % 8  // 右邻
            };

            // 6. 过滤（只保留 availableGrids 中存在的）
            foreach (var c in candidates)
            {
                for (var j = 0; j < availableGrids.Length; j++)
                {
                    if (availableGrids[j].value == c)
                    {
                        result.Add(c);
                        break;
                    }
                }
            }

            return result;
        }

    }
}