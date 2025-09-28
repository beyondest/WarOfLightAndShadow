using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public static class GameplayUIUtils
    {
        public static int CalMaxCountForConjureOrConstruct(
            EntityManager em, Entity targetEntity)
        {
            using var query = em.CreateEntityQuery(typeof(SubGameStatusData));
            var city = query.GetSingleton<SubGameStatusData>().City;
            var cityResourceEntries = em.GetBuffer<CityResourceEntry>(city);
            var costData = em.GetBuffer<CostList>(targetEntity);
            var minCount = int.MaxValue;
            foreach (var cost in costData)
            {
                var count = cityResourceEntries[(int)cost.Type].resourceData.availableAmount / cost.Amount;
                if (count < minCount)
                    minCount = count;
            }

            return minCount;
        }
    }
}