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

    public struct FactionUtils
    {
        public static Relationship GetRelationship(FactionTag selfFaction, SubFactionTag selfSubFaction,
            FactionTag targetFaction, SubFactionTag targetSubFaction)
        {
            if(targetFaction == FactionTag.Neutral) // Neutral faction has no sub faction
                return Relationship.Neutral;
            if (selfFaction == ~targetFaction)
                return Relationship.Hostile;
            // Light faction is ally to same general faction, dark faction is ally only when sub faction is same too
            return ContainsSubFaction(targetSubFaction, selfSubFaction) ? Relationship.Self : Relationship.Ally;
        }

        public static void ExpandSubFaction(SubFactionTag targetSubFaction,ref PlayerFactionData playerFactionData)
        {
            var intFaction = (int)playerFactionData.subFaction;
            intFaction |= (int)targetSubFaction;
            playerFactionData.subFaction = (SubFactionTag)intFaction;
        }

        private static bool ContainsSubFaction(SubFactionTag checkFaction, SubFactionTag selfSubFaction)
        {
            var intFaction = (int)selfSubFaction;
            return (intFaction & (int)checkFaction) != 0;
        }
    }
}