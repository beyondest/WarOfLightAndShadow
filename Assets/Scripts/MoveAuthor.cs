using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
    public class MoveAuthor : MonoBehaviour
    {
        private class MoveAuthorBaker : Baker<MoveAuthor>
        {
            public override void Bake(MoveAuthor authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<TestAttr>(entity);
            }
        }
    }

    public struct TestAttr : IComponentData
    {
        
    }
}