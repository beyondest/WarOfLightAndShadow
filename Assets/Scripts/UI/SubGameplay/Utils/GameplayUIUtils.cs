using System;
using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public static class GameplayUIUtils
    {
        public static int CalMaxCountForConjureOrConstruct(FactionTag faction,
            EntityManager em, Entity targetEntity)
        {
            EntityQuery query;
            switch (faction)
            {
                case FactionTag.Light:
                    query = em.CreateEntityQuery(typeof(LightResourceDataTag));
                    break;
                case FactionTag.Dark:
                    query = em.CreateEntityQuery(typeof(DarkResourceDataTag));
                    break;
                case FactionTag.Neutral:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var resourceEntity = query.GetSingletonEntity();
            var resourceData = em.GetBuffer<ResourceTypeToAvailableAmount>(resourceEntity);
            var costData = em.GetBuffer<CostList>(targetEntity);
            var minCount = int.MaxValue;
            foreach (var cost in costData)
            {
                var count = resourceData[(int)cost.Type].Amount / cost.Amount;
                if (count < minCount)
                    minCount = count;
            }
            return minCount;
        }
    }
}