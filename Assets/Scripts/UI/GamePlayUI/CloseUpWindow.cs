using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class CloseUpWindow : UIUtils.SingleTargetWindow
    {
        public static CloseUpWindow Instance;
        private Entity CloseUpTarget = Entity.Null;


        [Header("Custom Config")] public GameObject closeUpPanel;
        public Camera closeUpCamera;
        public RawImage closeUpImage;
        public Slider closeUpStatSlider;
        public Slider closeUpExpSlider;
        public TMP_Text closeUpTargetName;
        public Image closeUpTargetTier;
        public TMP_Text closeUpTargetExp;
        
        public Vector3 camBias;
        public LayerMask closeUpLayerMask;


        [Header("MultiShow Config")] public AssetReferenceGameObject buffSlotRef;
        public UIUtils.MultiShowSlotConfig config;
        
        public override void Show(Vector2? pos = null)
        {
            closeUpCamera.enabled = true;
            closeUpPanel.SetActive(true);
        }

        public override void Hide()
        {
            SetLayerRecursively(CloseUpTarget, _oriLayer);
            CloseUpTarget = Entity.Null;
            closeUpCamera.enabled = false;
            closeUpPanel.SetActive(false);
        }

        public override bool IsOpened()
        {
            return closeUpPanel.activeSelf;
        }

        public override bool TrySwitchTarget(Entity target)
        {
            // reset the last target layer
            SetLayerRecursively(CloseUpTarget, _oriLayer);
            if (!_em.HasComponent<InteractableAttr>(target)) return false;
            var attr = _em.GetComponentData<InteractableAttr>(target);
            _closeUpTargetColliderSize = attr.BoxColliderSize;
            CloseUpTarget = target;
            _oriLayer = SetLayerRecursively(CloseUpTarget, _closeUpLayerIndex);
            
            closeUpTargetName.text = attr.GameplayName.ToString();
            
       
            return true;
        }

        public override bool HasTarget()
        {
            return CloseUpTarget != Entity.Null;
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

        private AsyncOperationHandle<GameObject> _buffSlotHandle;
        // Cache
        private GameObject _buffSlotPrefab;
        private readonly List<GameObject> _buffSlots = new ();
        
        
        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;


        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            _buffSlotHandle = CR.LoadAssetRefAsync<GameObject>(buffSlotRef, go
                =>
            {
                _buffSlotPrefab = go;
                UIUtils.InitMultiShowSlotsByIndex(_buffSlots, closeUpPanel, _buffSlotPrefab, in config,
                    OnClickBuffSlot);
            });
        }

        private void OnDisable()
        {
            Addressables.Release(_buffSlotHandle);
            _buffSlots.Clear();
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            RenderTexture rt = new RenderTexture(
                (int)closeUpImage.rectTransform.rect.width,
                (int)closeUpImage.rectTransform.rect.height,
                16
            );
            closeUpCamera.targetTexture = rt;
            closeUpImage.texture = rt;
            _closeUpLayerIndex = (int)math.log2(closeUpLayerMask.value);
            Hide();
        }
        

        private void Update()
        {
            _cameraBias = camBias;
            if (_notPauseTag.IsEmpty) return;
            if (CloseUpTarget == Entity.Null) return;
            if (!IsOpened()) return;

            UpdateCloseUpShow();
        }

        private void UpdateCloseUpShow()
        {
            var tarTransform = _em.GetComponentData<LocalTransform>(CloseUpTarget);
            var statData = _em.GetComponentData<StatData>(CloseUpTarget);
            closeUpStatSlider.value = 1 - (float)statData.CurValue / statData.MaxValue;

            var x = _closeUpTargetColliderSize.x * 0.5f;
            var y = _closeUpTargetColliderSize.y;
            var z = _closeUpTargetColliderSize.z * 0.5f;
            closeUpCamera.transform.position = (Vector3)tarTransform.Position + _cameraBias + new Vector3(x, y, z);
            closeUpCamera.transform.LookAt(tarTransform.Position + new float3(0, 0.5f * y, 0));
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
                SetLayerRecursively(e, newLayer);
            }

            return oriLayer;
        }
    }
}