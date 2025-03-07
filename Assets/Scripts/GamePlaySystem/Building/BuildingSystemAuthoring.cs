using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingSystemAuthoring : MonoBehaviour
    {
        private class BuildingSystemAuthoringBaker : Unity.Entities.Baker<BuildingSystemAuthoring>
        {
            public override void Bake(BuildingSystemAuthoring authoring)
            {
            }
        }
    }




}