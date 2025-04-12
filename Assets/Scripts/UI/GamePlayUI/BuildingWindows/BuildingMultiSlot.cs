using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class BuildingMultiSlot : MultiShowSlot
    {
        [SerializeField] private TMP_Text gameplayNameText;


        private EntityManager _em;

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public override void SetTarget(Entity target)
        {
            var interactAttr = _em.GetComponentData<InteractableAttr>(target);
            gameplayNameText.text = interactAttr.GameplayName.ToString();
        }
    }
}