using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.VFX;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "VFXDatabase", menuName = "GameData/VFXDatabase", order = 0)]
    public class VFXDatabaseSo : ScriptableObject
    {
        [TableList] public List<VFXDataItem> items;
        
        
    }


    [Serializable]
    public class VFXDataItem
    {
        [VerticalGroup("General"), HorizontalGroup("General/0")]
        [AssetsOnly] public GameObject prefab;

        [VerticalGroup("General"), HorizontalGroup("General/1")]
        public VFXName name;

        [VerticalGroup("General"), HorizontalGroup("General/2"), HideLabel]
        public VFXType type;

        [VerticalGroup("General"), HorizontalGroup("General/3")]
        public bool killUntilAllStopPlay ;

        [VerticalGroup("General"), HorizontalGroup("General/3"), ShowIf(nameof(killUntilAllStopPlay))]
        public float maxWaitTimeForAllStopPlay;

        [VerticalGroup("SubFilter"), HorizontalGroup("SubFilter/0")]
        public bool hasFaction;

        [ShowIf(nameof(hasFaction))]
        [VerticalGroup("SubFilter"), HorizontalGroup("SubFilter/1")]
        public FactionTag faction;

        [InfoBox("If not has tier, then will be considered to be used as any tier, so as faction")]
        [VerticalGroup("SubFilter"), HorizontalGroup("SubFilter/2")]
        public bool hasTier;
        
        [VerticalGroup("SubFilter"), HorizontalGroup("SubFilter/3"), ShowIf(nameof(hasTier))]
        public Tier tier;
 
        [VerticalGroup("Projectile"), HorizontalGroup("Projectile/0")]
        [ShowIf(nameof(IsProjectile)),Tooltip("If this projectile has no hit effect, then just use None")]
        public VFXName hitEffectName = VFXName.None;

        [VerticalGroup("Projectile"), HorizontalGroup("Projectile/1")]
        [ShowIf(nameof(IsProjectile))]
        public float horizontalSpeed;

        [VerticalGroup("Projectile"), HorizontalGroup("Projectile/2"), ShowIf(nameof(IsProjectile))]
        public ProjectileType projectileType;
        
        [VerticalGroup("Projectile"), HorizontalGroup("Projectile/3")]
        [ShowIf(nameof(IsProjectile)),ShowIf(nameof(IsParabola)), Tooltip("If 0, then projectile is straight forward, otherwise is increased by distance")]
        public float baseRelativeHeight;
        
        
        [VerticalGroup("Projectile"), HorizontalGroup("Projectile/4")]
        [ShowIf(nameof(IsProjectile)), ShowIf(nameof(IsNotStopUntilReachMaxDis))]
        public float maxFlightDistance;

        [VerticalGroup("Projectile"), HorizontalGroup("Projectile/5")]
        [ShowIf(nameof(IsProjectile)), ShowIf(nameof(IsGoStraightWithHeightChange))]
        public float initialHeight;
        
        private bool IsProjectile()
        {
            return type == VFXType.Projectile;
        }

        private bool IsGoStraightWithHeightChange()
        {
            return projectileType == ProjectileType.GoStraightToTargetWithHeightChange;
        }
        private bool IsNotStopUntilReachMaxDis()
        {
            return projectileType == ProjectileType.NoHeightChangeUntilReachMaxDis;
        }

        private bool IsParabola()
        {
            return projectileType == ProjectileType.Parabola;
        }
    }
}