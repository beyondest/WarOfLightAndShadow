using System;
using Unity.Entities;

namespace SparFlame.Components.General
{

    public struct GeneralRandom : IComponentData
    {
        public Unity.Mathematics.Random Rnd;
    }

    [Serializable]
    public struct PlayerFactionData : IComponentData
    {
        public FactionTag faction;
        public SubFactionTag subFaction;
    }

    public struct PlayerSaveSlot : IComponentData
    {
        public int Value;
    }

}