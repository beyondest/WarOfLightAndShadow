using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.Command;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class BasicResourceManager : MonoBehaviour
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
        public static BasicResourceManager Instance;


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

        private void OnEnable()
        {
            var handle = CR.LoadTypeSuffix<Tier, Sprite>(tierSpriteSuffix,
                result =>
                {
                    CR.OnTypeSuffixLoadComplete(result, TierSprites);
                });
            _group.Add(handle);
            var handle1 = CR.LoadTypeSuffix<BuffType, Sprite>(buffSpriteSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, BuffSprites); });
            _group.Add(handle1);

            var handle2 = CR.LoadTypeSuffix<ResourceType, Sprite>(resourceTypeSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, ResourceSprites));
            _group.Add(handle2);


            var handle4 = CR.LoadTypeSuffix<FactionTag, Sprite>(factionHpSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpSprites));
            _group.Add(handle4);

            var handle3 = CR.LoadTypeSuffix<CursorType, Sprite>(cursorTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, CursorSprites); });
            _group.Add(handle3);

            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionGameOverSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionGameOverSprites)
                ));
            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionHpFillSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpFillSprites)));
            _group.Add(CR.LoadTypeSuffix<FactionTag, Sprite>(factionHpBlankSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, FactionHpBlankSprites)));
            
            
            // TODO : only enter game mode when all resource loaded; before that, show loading screen. So don't need to check resource loaded
        }

        private void OnDisable()
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