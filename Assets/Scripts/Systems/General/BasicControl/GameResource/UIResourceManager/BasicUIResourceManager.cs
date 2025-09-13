using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
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
        [SerializeField] [CanBeNull] private string mainGameplayCursorTypeSuffix;
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
        [SerializeField] private string ecoBuffSpriteSuffix;
        
        
        // Interfaces
        public static BasicUIResourceManager Instance;

        // public readonly Dictionary<BuffType, Sprite> BuffSprites = new();

        public readonly Dictionary<ResourceType, Sprite> ResourceSprites = new();
        public readonly Dictionary<Tier, Sprite> TierSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionGameOverSprites = new();
        public readonly Dictionary<SubGameplayCursorType, Sprite> SubGameplayCursorSprites = new();
        public readonly Dictionary<MainGameplayCursorType, Sprite> MainGameplayCursorSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpFillSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpBlankSprites = new();
        // public readonly Dictionary<WaveColorType,Sprite> LightWaveColorTypeSprites = new();
        // public readonly Dictionary<WaveColorType,Sprite> DarkWaveColorTypeSprites = new();
        // public readonly Dictionary<FactionTag,Sprite> FactionWaveTimeBasicSprites = new();   
        public readonly Dictionary<FactionTag,Sprite> FactionCrystalHpFilledSprites = new();
        public readonly Dictionary<FactionTag,Sprite> FactionCrystalHpBlankSprites = new();
        
        public readonly Dictionary<FactionTag, Sprite> GeneralFactionIconSprites = new();
        public readonly Dictionary<SubFactionTag, Sprite> SubFactionIconSprites = new();
        
        public readonly Dictionary<EcoType, Sprite> EcoBuffSprites = new();
        
        // Internal Data
        private readonly ResourceLoadingUtils.AddressableResourceGroup _group = new();

       

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

        public bool IsInitialized => _group.IsHandleCreated() && _group.IsDone;
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
                result => { ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, SubGameplayCursorSprites); }));

            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionGameOverSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionGameOverSprites)
            ));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionHpFillSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionHpFillSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionHpBlankSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionHpBlankSprites)));
            
            /*_group.Add(ResourceLoadingUtils.LoadTypeSuffix<WaveColorType,Sprite>(lightWaveColorTypeSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, LightWaveColorTypeSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<WaveColorType, Sprite>(darkWaveColorTypeSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, DarkWaveColorTypeSprites)));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag,Sprite>(factionWaveBasicSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionWaveTimeBasicSprites)));*/
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionCrystalHpFilledSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionCrystalHpFilledSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(factionCrystalHpBlankSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, FactionCrystalHpBlankSprites)));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<FactionTag, Sprite>(generalFactionCitySpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, GeneralFactionIconSprites)));
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<SubFactionTag, Sprite>(subFactionCitySpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, SubFactionIconSprites)));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<MainGameplayCursorType, Sprite>(mainGameplayCursorTypeSuffix,
                result => {ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, MainGameplayCursorSprites); }));
            
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<EcoType, Sprite>(ecoBuffSpriteSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, EcoBuffSprites)));
        }

        public void UnloadResources()
        {
            _group.Release();
            ResourceSprites.Clear();
            // BuffSprites.Clear();
            FactionHpSprites.Clear();
            FactionHpFillSprites.Clear();
            FactionHpBlankSprites.Clear();
            SubGameplayCursorSprites.Clear();
            TierSprites.Clear();
            
            // FactionWaveTimeBasicSprites.Clear();
            // DarkWaveColorTypeSprites.Clear();
            // LightWaveColorTypeSprites.Clear();

            FactionCrystalHpFilledSprites.Clear();
            FactionCrystalHpBlankSprites.Clear();
        
            GeneralFactionIconSprites.Clear();
            SubFactionIconSprites.Clear();
            
            EcoBuffSprites.Clear();
            
        }
    }
}