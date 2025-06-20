using Unity.Entities;

namespace SparFlame.Components.General
{

    public struct GeneralRandom : IComponentData
    {
        public Unity.Mathematics.Random Rnd;
    }

    public struct PlayerFactionData : IComponentData
    {
        public FactionTag Value;
    }

    public struct PlayerSaveSlot : IComponentData
    {
        public int Value;
    }

}