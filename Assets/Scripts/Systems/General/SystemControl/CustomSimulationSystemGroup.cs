using System;
using Unity.Entities;

namespace SparFlame.Systems.General.SystemControl
{

    public static class SystemOrderingConfig
    {
        public static Type[] Order =
        {

        };
    }
    public partial class CustomSimulationSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            foreach (var systemType in SystemOrderingConfig.Order)
            {
                var sys = World.CreateSystem(systemType);
                AddSystemToUpdateList(sys);
            }
            SortSystems();
        }
    }
    
}