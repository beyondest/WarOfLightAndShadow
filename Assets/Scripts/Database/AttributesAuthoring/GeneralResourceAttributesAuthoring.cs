using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.Resource;
using Unity.Entities;

namespace SparFlame.Database
{
    public class GeneralResourceAttributesAuthoring : GeneralDataItemAuthoring
    {
        public bool debugShow;
        protected class Baker : GeneralDataItemBaker<GeneralResourceAttributesAuthoring>
        {
            public override void Bake(GeneralResourceAttributesAuthoring authoring)
            {
                if(authoring.globalIdx == 0)return;
                var item = DatabaseManager.ResourceDatabaseSo.GetItemById(authoring.globalIdx);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                BakeGeneralDataItem(entity, item);
                AddComponent(entity, new ResourceAttr
                {
                    Type = item.type,
                    AmountRange = item.amountRange
                });
                if (item.renewable)
                {
                    AddComponent(entity, new RenewableData
                    {
                        RegeneratingLeftTime = 0f,
                        RegenerationTimeSeconds =  item.regenerationTimeSeconds,
                    });
                }
                BakeVolumeObstacleAttr(item, entity);
                if (!authoring.debugShow)
                {
                    AddComponent(entity, new HideFowAgentRequest
                    {
                        Hide = true
                    });
                }
            }
        }
    }
}