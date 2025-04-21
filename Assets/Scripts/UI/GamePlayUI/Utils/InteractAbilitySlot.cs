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
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void Start()
        {
        }

        protected override void Update()
        {
        }
    }
}