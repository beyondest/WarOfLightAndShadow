using Unity.Entities;
using UnityEngine;

namespace SparFlame.System.General
{
    public class AllSystemsConfigAuthoring : MonoBehaviour
    {
        class AllSystemsConfigAuthoringBaker : Baker<AllSystemsConfigAuthoring>
        {
            public override void Bake(AllSystemsConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<NotPauseTag>(entity);
            }
        }

        public void test()
        {
            Debug.Log("test");
        }
    }
    

    public struct NotPauseTag : IComponentData
    {
        
    }
}
