using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Command;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class BasicUIResourceManager : MonoBehaviour, CustomDs.IResourceManager
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
        public static BasicUIResourceManager Instance;

        public readonly Dictionary<BuffType, Sprite> BuffSprites = new();

        public readonly Dictionary<ResourceType, Sprite> ResourceSprites = new();
        public readonly Dictionary<Tier, Sprite> TierSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionGameOverSprites = new();
        public readonly Dictionary<CursorType, Sprite> CursorSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpFillSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpBlankSprites = new();
        private readonly AddressableResourceGroup _group = new();

        public bool IsResourceLoaded()
        {
            return _group.IsHandleCreated() && _group.IsDone;
        }

        private void Awake()
        {
            if (Instance == null)
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
            _group.Add(CR.LoadTypeSuffix<Tier, Sprite>(tierSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, TierSprites)));
            
            _group.Add(CR.LoadTypeSuffix<BuffType, Sprite>(buffSpriteSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, BuffSprites); }));

            _group.Add(CR.LoadTypeSuffix<ResourceType, Sprite>(resourceTypeSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, ResourceSprites)));

            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionHpSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpSprites)));

            _group.Add(CR.LoadTypeSuffix<CursorType, Sprite>(cursorTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, CursorSprites); }));

            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionGameOverSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionGameOverSprites)
            ));
            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionHpFillSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpFillSprites)));
            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionHpBlankSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpBlankSprites)));
        }

        public void UnloadResources()
        {
            _group.Release();
            ResourceSprites.Clear();
            BuffSprites.Clear();
            FactionHpSprites.Clear();
            FactionHpFillSprites.Clear();
            FactionHpBlankSprites.Clear();
            CursorSprites.Clear();
            TierSprites.Clear();
        }
    }
}