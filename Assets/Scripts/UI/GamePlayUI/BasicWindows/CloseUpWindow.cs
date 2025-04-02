using System.Collections.Generic;
using SparFlame.GamePlaySystem.Exp;
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
    public class CloseUpWindow : UIUtils.MultiSlotsWindow<BuffSlot,CloseUpWindow>,UIUtils.ISingleTargetWindow
    {

        // Config
        [Header("Custom Config")]
        [SerializeField]
        private Camera closeUpCamera;
        [SerializeField]
        private RawImage closeUpImage;
        [SerializeField]
        private Slider closeUpStatSlider;
        [SerializeField]
        private Slider closeUpExpSlider;
        [SerializeField]
        private TMP_Text closeUpTargetName;
        [SerializeField]
        private Image closeUpTargetTier;
        [SerializeField]
        private TMP_Text closeUpExpText;
        [SerializeField]
        private Vector3 camBias;
        [SerializeField]
        private LayerMask closeUpLayerMask;



        // Interface
        // public static CloseUpWindow Instance;

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            closeUpCamera.enabled = true;
        }

        public override void Hide()
        {
            base.Hide();
            SetLayerRecursively(_closeUpTarget, _oriLayer);
            _closeUpTarget = Entity.Null;
            closeUpCamera.enabled = false;
            closeUpExpSlider.enabled = false;
            closeUpExpText.enabled = false;
        }

  

        public bool TrySwitchTarget(Entity target)
        {
            if (!BasicWindowResourceManager.Instance.IsResourceLoaded()) return false;

            // reset the last target layer
            SetLayerRecursively(_closeUpTarget, _oriLayer);

            if (!_em.HasComponent<InteractableAttr>(target)) return false;
            var attr = _em.GetComponentData<InteractableAttr>(target);
            _closeUpTargetColliderSize = attr.BoxColliderSize;
            _closeUpTarget = target;

            // set target layer to closeup camera cull layer so that only this target is in view
            _oriLayer = SetLayerRecursively(_closeUpTarget, _closeUpLayerIndex);

            // Update close up window static value
            closeUpTargetName.text = attr.GameplayName.ToString();
            closeUpExpText.enabled =
                closeUpExpSlider.enabled = _closeUpTargetExpEnabled = _em.HasComponent<ExpData>(target);
            closeUpTargetTier.sprite = BasicWindowResourceManager.Instance.TierSprites[attr.Tier];


            return true;
        }

        public bool HasTarget()
        {
            return _closeUpTarget != Entity.Null;
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
        private bool _closeUpTargetExpEnabled;
        private Entity _closeUpTarget = Entity.Null;
        private AsyncOperationHandle<GameObject> _buffSlotHandle;

        
        // Cache
        private GameObject _buffSlotPrefab;


        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;


 

 

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            var rt = new RenderTexture(
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
            if (_closeUpTarget == Entity.Null) return;
            if (!IsOpened()) return;
            if (!BasicWindowResourceManager.Instance.IsResourceLoaded()) return;

            UpdateCloseUpShow();
        }

        private void UpdateCloseUpShow()
        {
            // Update Hp
            var statData = _em.GetComponentData<StatData>(_closeUpTarget);
            closeUpStatSlider.value = 1 - (float)statData.CurValue / statData.MaxValue;
            
            // Update Exp
            if (_closeUpTargetExpEnabled)
            {
                var expData = _em.GetComponentData<ExpData>(_closeUpTarget);
                closeUpExpSlider.value = (float)expData.CurValue / expData.MaxValue;
                closeUpExpText.text = expData.CurValue + "/" + expData.MaxValue;
            }

            // Update camera close up show
            var tarTransform = _em.GetComponentData<LocalTransform>(_closeUpTarget);
            var x = _closeUpTargetColliderSize.x * 0.5f;
            var y = _closeUpTargetColliderSize.y;
            var z = _closeUpTargetColliderSize.z * 0.5f;
            closeUpCamera.transform.position = (Vector3)tarTransform.Position + _cameraBias + new Vector3(x, y, z);
            closeUpCamera.transform.LookAt(tarTransform.Position + new float3(0, 0.5f * y, 0));
            
            // Update BuffData
            if (_em.HasComponent<BuffData>(_closeUpTarget))
            {
                var buffData = _em.GetBuffer<BuffData>(_closeUpTarget);
                var count = Mathf.Min(Slots.Count, buffData.Length);
                for (var i = 0; i < Slots.Count; i++)
                {
                    if (i < count)
                    {
                        Slots[i].SetActive(true);
                        var buff = SlotComponents[i];
                        buff.button.image.sprite = BasicWindowResourceManager.Instance.BuffSprites[buffData[i].Type];
                    }
                    else
                    {
                        Slots[i].SetActive(false);
                    }
                }
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
                SetLayerRecursively(e, newLayer);
            }

            return oriLayer;
        }
    }
}