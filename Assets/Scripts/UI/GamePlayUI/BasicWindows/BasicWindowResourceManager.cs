using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class BasicWindowResourceManager : CustomResourceManager
    {

        [SerializeField]
        [CanBeNull] private string tierSpriteSuffix;
        [SerializeField]
        [CanBeNull] private string buffSpriteSuffix;
        [SerializeField]
        [CanBeNull] private string resourceTypeSpriteSuffix;
        [SerializeField]
        [CanBeNull] private string factionTypeHpSpriteSuffix = "Hp";

        
        public static BasicWindowResourceManager Instance;
        
        
        public readonly Dictionary<Tier, Sprite> TierSprites = new ();
        public readonly Dictionary<BuffType, Sprite> BuffSprites = new();
        public readonly Dictionary<ResourceType, Sprite> ResourceSprites = new();
        public readonly Dictionary<FactionTag, Sprite> FactionHpSprites = new();

        
        private readonly AddressableResourceGroup _group = new();
        
        public override bool IsResourceLoaded()
        {
            return _group.IsHandleCreated(4) && _group.IsDone;
        }

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            var handle = CR.LoadTypeSuffix<Tier, Sprite>(tierSpriteSuffix,
                result =>
                {
                    CR.OnTypeSuffixLoadComplete(result, TierSprites);
                });
            _group.Add(handle);
            var handle2 = CR.LoadTypeSuffix<BuffType, Sprite>(buffSpriteSuffix,
                result =>
                {
                    CR.OnTypeSuffixLoadComplete(result, BuffSprites);
                });
            _group.Add(handle2);
            
            var handle3 = CR.LoadTypeSuffix<ResourceType, Sprite>(resourceTypeSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, ResourceSprites));
            _group.Add(handle3);
            
            
            var handle4 = CR.LoadTypeSuffix<FactionTag, Sprite>(factionTypeHpSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpSprites));
            _group.Add(handle4);

        }

        private void OnDisable()
        {
            _group.Release();
            ResourceSprites.Clear();
            TierSprites.Clear();
            BuffSprites.Clear();
            FactionHpSprites.Clear();
        }
        
    }
}