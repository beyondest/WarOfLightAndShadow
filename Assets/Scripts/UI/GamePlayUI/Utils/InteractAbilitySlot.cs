using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class InteractAbilitySlot : InteractAbilityWindow
    {
        protected override void Awake()
        {
        }

        private void OnEnable()
        {
            Show();
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void Start()
        {
        }

        protected override void Update()
        {
        }
    }
}