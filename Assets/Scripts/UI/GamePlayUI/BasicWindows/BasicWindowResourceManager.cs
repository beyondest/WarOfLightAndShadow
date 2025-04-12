using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.Command;
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
        [SerializeField] [CanBeNull] private string buffSpriteSuffix;
        [SerializeField] [CanBeNull] private string resourceTypeSpriteSuffix;
        [SerializeField] [CanBeNull] private string cursorTypeSuffix;
        [SerializeField] [CanBeNull] private string resourceStateSpriteSuffix;

        public static BasicWindowResourceManager Instance;


        // public readonly Dictionary<Tier, Sprite> TierSprites = new ();
        public readonly Dictionary<BuffType, Sprite> BuffSprites = new();

        public readonly Dictionary<ResourceType, Sprite> ResourceSprites = new();

        // public readonly Dictionary<FactionTag, Sprite> FactionHpSprites = new();
        public readonly Dictionary<CursorType, Sprite> CursorSprites = new();


        private readonly AddressableResourceGroup _group = new();

        public override bool IsResourceLoaded()
        {
            return _group.IsHandleCreated(3) && _group.IsDone;
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
            // var handle = CR.LoadTypeSuffix<Tier, Sprite>(tierSpriteSuffix,
            //     result =>
            //     {
            //         CR.OnTypeSuffixLoadComplete(result, TierSprites);
            //     });
            // _group.Add(handle);
            var handle1 = CR.LoadTypeSuffix<BuffType, Sprite>(buffSpriteSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, BuffSprites); });
            _group.Add(handle1);

            var handle2 = CR.LoadTypeSuffix<ResourceType, Sprite>(resourceTypeSpriteSuffix,
                result => CR.OnTypeSuffixLoadComplete(result, ResourceSprites));
            _group.Add(handle2);


            // var handle4 = CR.LoadTypeSuffix<FactionTag, Sprite>(factionTypeHpSpriteSuffix,
            //     result => CR.OnTypeSuffixLoadComplete(result, FactionHpSprites));
            // _group.Add(handle4);

            var handle3 = CR.LoadTypeSuffix<CursorType, Sprite>(cursorTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, CursorSprites); });
            _group.Add(handle3);

            // TODO : only enter game mode when all resource loaded; before that, show loading screen. So don't need to check resource loaded
        }

        private void OnDisable()
        {
            _group.Release();
            ResourceSprites.Clear();
            // TierSprites.Clear();
            BuffSprites.Clear();
            // FactionHpSprites.Clear();
            CursorSprites.Clear();
        }
    }
}