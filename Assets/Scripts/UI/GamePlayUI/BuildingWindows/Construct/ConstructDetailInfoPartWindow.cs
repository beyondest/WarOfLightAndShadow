using SparFlame.BootStrapper;
using Unity.Entities;

namespace SparFlame.UI.GamePlay.UI.GamePlayUI.BuildingWindows.Construct
{
    public class ConstructDetailInfoPartWindow : BuildingDetailWindow
    {
        protected override void Awake()
        {
        }
        

        public override void LoadResources()
        {
            base.LoadResources();
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