using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.SubGameplay
{
    public class SubGameplayCloseUpWindow : MultiSlotWindowUtils.MultiSlotsWindow<BuffSlot>,
        MultiSlotWindowUtils.ISingleTargetWindow
    {
        // Config
        [Header("Close Up Config")] [SerializeField]
        private TMP_Text closeUpTargetName;
        [SerializeField] private Camera closeUpCamera;
        [SerializeField] private RawImage closeUpRawImage;
        [SerializeField] private LayerMask closeUpLayerMask;
        [SerializeField] private Vector3 camBias;

        [Header("Exp ")] [SerializeField] private GameObject lightExpObj;
        [SerializeField] private GameObject darkExpObj;
        [SerializeField] private Image lightExpFilled;
        [SerializeField] private Image darkExpFilled;
        [SerializeField] private TMP_Text expValueText;
        [SerializeField] private Image tierIcon;
        [SerializeField] private TMP_Text levelText;

        [Header("Hp")] [SerializeField] private GameObject lightHpObj;
        [SerializeField] private Image lightHpFilled;
        [SerializeField] private GameObject darkHpObj;
        [SerializeField] private Image darkHpFilled;
        [SerializeField] private GameObject neutralHpObj;
        [SerializeField] private Image neutralHpFilled;
        [SerializeField] private TMP_Text statValueText;
        [SerializeField] private TMP_Text statLabelText;
        // [SerializeField] private TMP_Text statBonusText;
        

        // Interface
        public static SubGameplayCloseUpWindow Instance;

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            closeUpCamera.enabled = true;
            if (_targetEntity != Entity.Null)
                SetLayerRecursively(_targetEntity, _closeUpLayerIndex);
        }

        public override void Hide()
        {
            base.Hide();
            closeUpCamera.enabled = false;
            SetLayerRecursively(_targetEntity, _oriLayer);
            lightExpObj.SetActive(false);
            darkExpObj.SetActive(false);
            darkHpObj.SetActive(false);
            lightHpObj.SetActive(false);
            neutralHpObj.SetActive(false);
            tierIcon.enabled = false;
        }


        public bool TrySwitchTarget(Entity target)
        {
            // reset the last target layer
            SetLayerRecursively(_targetEntity, _oriLayer);
            if (!_em.HasComponent<SubGameplayGeneralAttr>(target)) return false;
            _showExpBar =  _em.HasComponent<UnitAttr>(target);
            _targetEntity = target;
            // set target layer to closeup camera cull layer so that only this target is in view
            _oriLayer = SetLayerRecursively(_targetEntity, _closeUpLayerIndex);
            // Update close up window static value
            UpdateStaticData();
            return true;
        }


        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            SetLayerRecursively(_targetEntity, _oriLayer);
            _targetEntity = Entity.Null;
        }


        public void OnClickBuffSlot(int slotIndex)
        {
            Debug.Log($"Click on {slotIndex}");
        }

        // Internal Data
        private int _closeUpLayerIndex;
        private int _oriLayer;
        private Vector3 _cameraBias;
        private Vector3 _closeUpTargetColliderSize = Vector3.zero;
        private bool _showExpBar;
        private Entity _targetEntity = Entity.Null;


        // Cache
        private GameObject _buffSlotPrefab;
        private Image _expFilled;
        private Image _statFilled;


        // ECS
        private EntityManager _em;
        private EntityQuery _gamingTag;


        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }


        protected override void Start()
        {
            base.Start();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            if(_gamingTag != default)   _gamingTag.Dispose();
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            var rt = new RenderTexture(
                (int)closeUpRawImage.rectTransform.rect.width,
                (int)closeUpRawImage.rectTransform.rect.height,
                16
            );
            closeUpCamera.targetTexture = rt;
            closeUpRawImage.texture = rt;
            _closeUpLayerIndex = (int)math.log2(closeUpLayerMask.value);
            Hide();
        }


        private void Update()
        {
            _cameraBias = camBias;
            if (_gamingTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null)
            {
                Hide();
            }

            if (!_em.HasComponent<StatData>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                Hide();
                return;
            }

            UpdateDynamicData();
        }

        private void OnDestroy()
        {
            if(_gamingTag != default)   _gamingTag.Dispose();

        }

        private void UpdateDynamicData()
        {
            // Update Hp
            var statData = _em.GetComponentData<StatData>(_targetEntity);
            _statFilled.fillAmount = statData.curValue / statData.maxValue /*+ statData.bonus*/;
            // if (statData.bonus == 0)
            // {
            //     statBonusText.enabled = false;
            // }
            // else
            // {
            //     statBonusText.enabled = true;
            //     var signal = statData.bonus > 0 ? "+" : "-";
            //     statBonusText.text = $"({signal}{statData.bonus})";
            //     statBonusText.color = statData.bonus > 0 ? Color.green : Color.red;
            // }

            statValueText.text = (int)statData.curValue + " / " + statData.maxValue /*+ statData.bonus*/;
            // Update Exp
            if (_showExpBar)
            {
                var expData = _em.GetComponentData<ExpData>(_targetEntity);
                _expFilled.fillAmount = expData.curValue / expData.maxValue;
                expValueText.text = (int)expData.curValue + "/" + expData.maxValue;
                var countLevel = expData.curLevel + ((int)expData.curTier - 3) * 10;
                levelText.text = $"Lv. {countLevel}";
            }

            // Update camera close up show
            var tarTransform = _em.GetComponentData<LocalTransform>(_targetEntity);
            var x = _closeUpTargetColliderSize.x * 0.5f;
            var y = _closeUpTargetColliderSize.y;
            var z = _closeUpTargetColliderSize.z * 0.5f;
            closeUpCamera.transform.position = (Vector3)tarTransform.Position + _cameraBias + new Vector3(x, y, z);
            closeUpCamera.transform.LookAt(tarTransform.Position + new float3(0, 0.5f * y, 0));

            // Update BuffData
            // if (_em.HasComponent<BuffData>(_targetEntity))
            // {
            //     var buffData = _em.GetBuffer<BuffData>(_targetEntity);
            //     var count = Mathf.Min(Slots.Count, buffData.Length);
            //     for (var i = 0; i < Slots.Count; i++)
            //     {
            //         if (i < count)
            //         {
            //             Slots[i].SetActive(true);
            //             var buff = SlotComponents[i];
            //             // buff.button.image.sprite = BasicUIResourceManager.Instance.BuffSprites[buffData[i].Type];
            //         }
            //         else
            //         {
            //             Slots[i].SetActive(false);
            //         }
            //     }
            // }
        }


        private void UpdateStaticData()
        {
            var attr = _em.GetComponentData<SubGameplayGeneralAttr>(_targetEntity);
            var prefabId = _em.GetComponentData<PrefabId>(_targetEntity);
            _closeUpTargetColliderSize = _em.GetComponentData<BoxColliderSize>(_targetEntity).SeparationBox;
            closeUpTargetName.text = attr.BaseTag switch
            {
                BaseTag.Units => DatabaseManager.UnitDatabaseSo.GetItemById(prefabId.value).gameplayName,
                BaseTag.Buildings => DatabaseManager.BuildingDatabaseSo.GetItemById(prefabId.value).gameplayName,
                BaseTag.Resources => DatabaseManager.ResourceDatabaseSo.GetItemById(prefabId.value).gameplayName,
                _ => BurstSafe.UnexpectedEnum(attr.BaseTag, "Wrong")
            };
            if (_em.HasComponent<ExpData>(_targetEntity))
            {
                var tier = _em.GetComponentData<ExpData>(_targetEntity).curTier;
                tierIcon.sprite = BasicUIResourceManager.Instance.TierSprites[tier];
                tierIcon.enabled = true;
            }
            else
            {
                tierIcon.enabled = false;
            }

            switch (attr.Faction)
            {
                case FactionTag.Light:
                    lightHpObj.SetActive(true);
                    darkHpObj.SetActive(false);
                    lightExpObj.SetActive(_showExpBar);
                    darkExpObj.SetActive(false);
                    neutralHpObj.SetActive(false);
                    _statFilled = lightHpFilled;
                    _expFilled = lightExpFilled;
                    statLabelText.text = "Hp";
                    statValueText.enabled = true;
                    tierIcon.color = Color.white;

                    break;
                case FactionTag.Dark:
                    lightHpObj.SetActive(false);
                    darkHpObj.SetActive(true);
                    lightExpObj.SetActive(false);
                    darkExpObj.SetActive(_showExpBar);
                    neutralHpObj.SetActive(false);
                    _statFilled = darkHpFilled;
                    _expFilled = darkExpFilled;
                    statLabelText.text = "Hp";
                    statValueText.enabled = true;
                    tierIcon.color = Color.black;
                    break;
                case FactionTag.Neutral:
                    lightExpObj.SetActive(false);
                    darkExpObj.SetActive(false);
                    darkHpObj.SetActive(false);
                    lightHpObj.SetActive(false);
                    neutralHpObj.SetActive(true);

                    _expFilled = lightExpFilled;
                    _statFilled = neutralHpFilled;
                    statLabelText.text = "";
                    statValueText.enabled = false;
                    tierIcon.color = Color.yellow;
                    break;
                default:
                    BurstSafe.UnexpectedEnum(attr.Faction);
                    break;
            }
        }

        /// <summary>
        /// Set all child entity in parent entity to specified layer  
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newLayer"></param>
        /// <returns>Original layer of entity, notice that all child entities should be in the same layer of parent entity</returns>
        private int SetLayerRecursively(Entity entity, int newLayer)
        {
            var oriLayer = 0;
            if (!_em.HasComponent<LinkedEntityGroup>(entity))
                return oriLayer;
            var buffer = _em.GetBuffer<LinkedEntityGroup>(entity);
            var entities = new List<Entity>();
            foreach (var linkedEntity in buffer)
            {
                if (!_em.HasComponent<RenderFilterSettings>(linkedEntity.Value)) continue;
                entities.Add(linkedEntity.Value);
            }

            foreach (var e in entities)
            {
                var renderFilter = _em.GetSharedComponent<RenderFilterSettings>(e);
                oriLayer = renderFilter.Layer;
                renderFilter.Layer = newLayer;
                _em.SetSharedComponent(e, renderFilter);
                if (e == entity) continue;
                SetLayerRecursively(e, newLayer);
            }

            return oriLayer;
        }

        [Serializable]
        public struct TierImagePair
        {
            public Tier tier;
            public Image image;
        }
    }
}