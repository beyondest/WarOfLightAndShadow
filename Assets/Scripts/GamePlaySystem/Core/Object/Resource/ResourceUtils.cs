using System;
using System.Runtime.CompilerServices;

namespace SparFlame.GamePlaySystem.Resource
{
    public struct ResourceUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetCorrespondingResource(ResourceType resourceType, out ResourceType correspondingResourceType)
        {
            correspondingResourceType = resourceType;
            switch (resourceType)
            {
                case ResourceType.SoulPact:
                case ResourceType.Essence:
                case ResourceType.Aetherium:
                case ResourceType.BloodCrystal:
                case ResourceType.SoulMist:
                case ResourceType.ArcaneEnergy:
                case ResourceType.ChaosShard:
                case ResourceType.RelicFragments:
                    return false;
                case ResourceType.LightEnergy:
                    correspondingResourceType = ResourceType.DarkEnergy;
                    return true;
                case ResourceType.DarkEnergy:
                    correspondingResourceType = ResourceType.LightEnergy;
                    return true;
                case ResourceType.Luminite:
                    correspondingResourceType = ResourceType.Obsidian;
                    return true;
                case ResourceType.Obsidian:
                    correspondingResourceType = ResourceType.Luminite;
                    return true;
                case ResourceType.StarLight:
                    correspondingResourceType = ResourceType.NetherFlame;
                    return true;
                case ResourceType.NetherFlame:
                    correspondingResourceType = ResourceType.StarLight;
                    return true;
                case ResourceType.OathOfLight:
                    correspondingResourceType = ResourceType.ShadowCovenant;
                    return true;
                case ResourceType.ShadowCovenant:
                    correspondingResourceType = ResourceType.OathOfLight;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }   
        }
    }
}