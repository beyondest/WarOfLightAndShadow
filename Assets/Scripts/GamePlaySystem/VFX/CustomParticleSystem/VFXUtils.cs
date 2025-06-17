using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;

namespace SparFlame.GamePlaySystem.CustomParticleSystem
{
    public struct VFXUtils
    {
        // public static VFXName GetProjectileVFXNameByAttr(in UnitAttr unitAttr,
        //     in BuildingAttr buildingAttr, in SubGameplayGeneralAttr subGameplayGeneralAttr)
        // {
        //     if (subGameplayGeneralAttr.BaseTag == BaseTag.Buildings)                                                                                                                                                                                   
        //     {
        //         switch (buildingAttr.SubTypeIndex)
        //         {
        //             case (int)FortificationType.Tower:
        //                 return VFXName.TowerProjectile;
        //         }
        //     }
        //     if (subGameplayGeneralAttr.BaseTag == BaseTag.Units)
        //     {
        //         switch (unitAttr.Type)
        //         {
        //             case UnitType.Ranged:
        //                 return VFXName.RangedUnitProjectile;
        //             case UnitType.Magic:
        //                 return VFXName.MagicSwordProjectile1;
        //             case UnitType.Shield:
        //             case UnitType.Cavalry:
        //             case UnitType.Worker:
        //                 break;
        //             default:
        //                 throw new ArgumentOutOfRangeException();
        //         }
        //     }
        //     return VFXName.None;
        // }
    }
}