using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.General.GeneralGameplayUI.PopupWindows.BattleCheckOutPage
{
    public class CrystalPrefabAuthoring : MonoBehaviour
    {
        public GameObject lightCrystalPrefab;
        public GameObject darkCrystalPrefab;
        private class CrystalPrefabAuthoringBaker : Baker<CrystalPrefabAuthoring>
        {
            public override void Bake(CrystalPrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CrystalPrefab
                {
                    LightCrystalPrefab =  GetEntity(authoring.lightCrystalPrefab, TransformUsageFlags.Dynamic),
                    DarkCrystalPrefab =  GetEntity(authoring.darkCrystalPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

 
}