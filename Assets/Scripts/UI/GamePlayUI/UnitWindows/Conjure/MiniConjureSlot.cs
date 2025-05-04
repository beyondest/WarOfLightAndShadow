using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class MiniConjureSlot : MultiShowSlot
    {
        [SerializeField]
        private Color cannotConjureColor = Color.gray;
        [SerializeField]
        private TMP_Text conjureMaxCountText;
        
        private EntityManager _em;
        private FactionTag _faction;
        private Entity _targetEntity = Entity.Null;
        private EntityQuery _gamingTag;
        private int _maxConjureCount;
        public int GetMaxConjureCount() => _maxConjureCount;
        public void SetTarget(in SpriteEntityInfo info)
        {
            _targetEntity = info.EntityPrefab;
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _faction = _em.GetComponentData<GeneralAttr>(info.EntityPrefab).FactionTag;
            button!.image.sprite = info.Sprite;
            CalculateMaxConjureCount();
        }
        
        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(GamingTag));
        }

        private void Update()
        {
            if(_gamingTag.IsEmpty)return;
            if(_targetEntity == Entity.Null)return;
            CalculateMaxConjureCount();
        }

        private void CalculateMaxConjureCount()
        {
            _maxConjureCount =GameplayUIUtils.CalMaxCountForConjureOrConstruct(_faction,
                _em, _targetEntity);
            button!.image.color = _maxConjureCount == 0 ? cannotConjureColor : Color.white;
            conjureMaxCountText.text = _maxConjureCount.ToString();
        }
    }
}