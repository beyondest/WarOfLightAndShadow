using System;
using System.Collections.Generic;
using SparFlame.Database;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class CloseUpWindow : MultiSlotWindowUtils.MultiSlotsWindow<BuffSlot>, MultiSlotWindowUtils.ISingleTargetWindow
    {
        // Config
        [Header("Custom Config")] [SerializeField]
        private Camera closeUpCamera;

        [SerializeField] private RawImage closeUpRawImage;

        [SerializeField] private GameObject lightExpObj;
        [SerializeField] private GameObject lightHpObj;
        [SerializeField] private GameObject darkExpObj;
        [SerializeField] private GameObject darkHpObj;
        [SerializeField] private GameObject neutralHpObj;
        [SerializeField] private Image lightExpFilled;
        [SerializeField] private Image lightHpFilled;
        [SerializeField] private Image darkExpFilled;
        [SerializeField] private Image darkHpFilled;
        [SerializeField] private Image neutralHpFilled;


        [SerializeField] private TMP_Text statValueText;
        [SerializeField] private TMP_Text expValueText;
        [SerializeField] private TMP_Text statLabelText;
        [SerializeField] private TMP_Text closeUpTargetName;
        [SerializeField] private TMP_Text statBonusText;

        // [SerializeField] private List<TierImagePair> tierImagePairs;
        [SerializeField] private Image tierIcon;

        [SerializeField] private Vector3 camBias;

        [SerializeField] private LayerMask closeUpLayerMask;

        // Interface
        public static CloseUpWindow Instance;

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
            if (!_em.HasComponent<GeneralAttr>(target)) return false;
            _targetHasExp = _em.HasComponent<ExpData>(target);
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
        private bool _targetHasExp;
        private Entity _targetEntity = Entity.Null;
        private AsyncOperationHandle<GameObject> _buffSlotHandle;


        // Cache
        private GameObject _buffSlotPrefab;
        private Image _expFilled;
        private Image _statFilled;


        // ECS
        private EntityManager _em;
        private EntityQuery _gamingTag;


        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        protected override void Start()
        {
            base.Start();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(GamingTag));
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
            // if (!BasicUIResourceManager.Instance.IsResourceLoaded()) return;
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

        private void UpdateDynamicData()
        {
            // Update Hp
            var statData = _em.GetComponentData<StatData>(_targetEntity);
            _statFilled.fillAmount = statData.CurValue / (statData.MaxValue + statData.Bonus);
            if (statData.Bonus == 0)
            {
                statBonusText.enabled = false;
            }
            else
            {
                statBonusText.enabled = true;
                var signal = statData.Bonus > 0 ? "+" : "-";
                statBonusText.text = $"({signal}{statData.Bonus})";
                statBonusText.color = statData.Bonus > 0 ? Color.green : Color.red;
            }
            statValueText.text = (int)statData.CurValue + " / " + (statData.MaxValue + statData.Bonus) ;
            // Update Exp
            if (_targetHasExp)
            {
                var expData = _em.GetComponentData<ExpData>(_targetEntity);
                _expFilled.fillAmount = expData.CurValue / expData.MaxValue;
                expValueText.text = (int)expData.CurValue + "/" + expData.MaxValue;
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
            var attr = _em.GetComponentData<GeneralAttr>(_targetEntity);
            _closeUpTargetColliderSize = attr.BoxColliderSize;
            closeUpTargetName.text = attr.BaseTag switch
            {
                BaseTag.Units => DatabaseManager.UnitDatabaseSo.GetItemById(attr.ID).gameplayName,
                BaseTag.Buildings => DatabaseManager.BuildingDatabaseSo.GetItemById(attr.ID).gameplayName,
                BaseTag.Resources => DatabaseManager.ResourceDatabaseSo.GetItemById(attr.ID).gameplayName,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (_em.HasComponent<ExpData>(_targetEntity))
            {
                var tier = _em.GetComponentData<ExpData>(_targetEntity).CurTier;
                tierIcon.sprite = BasicUIResourceManager.Instance.TierSprites[tier];
            }
            else
            {
                tierIcon.enabled = false;
            }
            switch (attr.FactionTag)
            {
                case FactionTag.Ally:
                    lightHpObj.SetActive(true);
                    darkHpObj.SetActive(false);
                    lightExpObj.SetActive(_targetHasExp);
                    darkExpObj.SetActive(false);
                    neutralHpObj.SetActive(false);
                    _statFilled = lightHpFilled;
                    _expFilled = lightExpFilled;
                    statLabelText.text = "Hp";
                    statValueText.enabled = true;
                    tierIcon.color = Color.white;

                    break;
                case FactionTag.Enemy:
                    lightHpObj.SetActive(false);
                    darkHpObj.SetActive(true);
                    lightExpObj.SetActive(false);
                    darkExpObj.SetActive(_targetHasExp);
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
                    throw new ArgumentOutOfRangeException();
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
                if(e == entity)continue;
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