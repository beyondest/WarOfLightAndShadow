using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay.UI.GamePlayUI.BuildingWindows.Construct
{
    public class ConstructDetailInfoSlot : BuildingDetailWindow
    {
        protected override void Awake()
        {
            
        }

        protected override void OnEnable()
        {
            base.OnEnable();
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