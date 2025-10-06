using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.City
{
    public class FocusResourceAuthoring : MonoBehaviour
    {
        private class FocusResourceAuthoringBaker : Baker<FocusResourceAuthoring>
        {
            public override void Bake(FocusResourceAuthoring authoring)
            {
            }
        }
    }
    
}