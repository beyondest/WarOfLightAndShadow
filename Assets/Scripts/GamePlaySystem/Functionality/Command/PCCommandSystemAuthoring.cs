using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Command
{
    public class PCCommandSystemAuthoring : MonoBehaviour
    {
        private class PCCommandSystemAuthoringBaker : Baker<PCCommandSystemAuthoring>
        {
            public override void Bake(PCCommandSystemAuthoring authoring)
            {
            }
        }
    }
    
    
}