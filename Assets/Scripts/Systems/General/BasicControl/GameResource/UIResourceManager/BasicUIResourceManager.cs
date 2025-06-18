using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.UI;
using SparFlame.Core.Interfaces;
using SparFlame.Core.Utils;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class BasicUIResourceManager : MonoBehaviour, IResourceManager
    {
        [SerializeField] [CanBeNull] private string buffSpriteSuffix;
        [SerializeField] [CanBeNull] private string resourceTypeSpriteSuffix;
        [SerializeField] [CanBeNull] private string cursorTypeSuffix;
        [SerializeField] [CanBeNull] private string resourceStateSpriteSuffix;
        [SerializeField] [CanBeNull] private string tierSpriteSuffix;
        [SerializeField] [CanBeNull] private string factionHpSpriteSuffix;
        [SerializeField] [CanBeNull] private string factionGameOverSpriteSuffix;
        [SerializeField] [CanBeNull] private string factionHpFillSpriteSuffix;
        [SerializeField] [CanBeNull] private string factionHpBlankSpriteSuffix;
        [SerializeField] private string lightWaveColorTypeSpriteSuffix;
        [SerializeField] private string darkWaveColorTypeSpriteSuffix;
        [SerializeField] private string factionWaveBasicSpriteSuffix;
        [SerializeField] private string factionCrystalHpFilledSpriteSuffix;
        [SerializeField] private string factionCrystalHpBlankSpriteSuffix;
        [SerializeField] private string generalFactionCitySpriteSuffix;
        [SerializeField] private string subFactionCitySpriteSuffix;

        public static BasicUIResourceManager Instance;

        // public readonly Dictionary<BuffType, Sprite> BuffSprites = new();

        public readonly Dictionary<ResourceType, Sprite> ResourceSprites = new();
        public readonly Dictionary<Tier, Sprite> TierSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionGameOverSprites = new();
        public readonly Dictionary<SubGameplayCursorType, Sprite> CursorSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpFillSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpBlankSprites = new();
        public readonly Dictionary<WaveColorType,Sprite> LightWaveColorTypeSprites = new();
        public readonly Dictionary<WaveColorType,Sprite> DarkWaveColorTypeSprites = new();
        public readonly Dictionary<FactionTag,Sprite> FactionWaveTimeBasicSprites = new();   
        public readonly Dictionary<FactionTag,Sprite> FactionCrystalHpFilledSprites = new();
        public readonly Dictionary<FactionTag,Sprite> FactionCrystalHpBlankSprites = new();
        
        public readonly Dictionary<FactionTag, Sprite> GeneralFactionIconSprites = new();
        public readonly Dictionary<SubFaction, Sprite> SubFactionIconSprites = new();
        
        private readonly ResourceLoadingUtils.AddressableResourceGroup _group = new();

        public bool IsResourceLoaded()
        {
            return _group.IsHandleCreated() && _group.IsDone;
        }

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
        }

        public bool IsInitialized => IsResourceLoaded();
        public float InitProgress => _group.AverageProgress;
        public void LoadResources()
        {
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<Tier, Sprite>(tierSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, TierSprites)));
            
            // _group.Add(CR.LoadTypeSuffix<BuffType, Sprite>(buffSpriteSuffix,
            //     result => { CR.OnTypeSuffixLoadComplete(result, BuffSprites); }));

            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<ResourceType, Sprite>(resourceTypeSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, ResourceSprites)));

            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionHpSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionHpSprites)));

            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<SubGameplayCursorType, Sprite>(cursorTypeSuffix,
                result => { ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, CursorSprites); }));

            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionGameOverSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionGameOverSprites)
            ));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionHpFillSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionHpFillSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionHpBlankSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionHpBlankSprites)));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<WaveColorType,Sprite>(lightWaveColorTypeSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, LightWaveColorTypeSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<WaveColorType, Sprite>(darkWaveColorTypeSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, DarkWaveColorTypeSprites)));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag,Sprite>(factionWaveBasicSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionWaveTimeBasicSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionCrystalHpFilledSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionCrystalHpFilledSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionCrystalHpBlankSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionCrystalHpBlankSprites)));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(generalFactionCitySpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, GeneralFactionIconSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<SubFaction, Sprite>(subFactionCitySpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, SubFactionIconSprites)));
        }

        public void UnloadResources()
        {
            _group.Release();
            ResourceSprites.Clear();
            // BuffSprites.Clear();
            FactionHpSprites.Clear();
            FactionHpFillSprites.Clear();
            FactionHpBlankSprites.Clear();
            CursorSprites.Clear();
            TierSprites.Clear();
            
            FactionWaveTimeBasicSprites.Clear();
            FactionCrystalHpFilledSprites.Clear();
            FactionCrystalHpBlankSprites.Clear();
            DarkWaveColorTypeSprites.Clear();
            LightWaveColorTypeSprites.Clear();
            
            GeneralFactionIconSprites.Clear();
            SubFactionIconSprites.Clear();
            
        }
    }
}