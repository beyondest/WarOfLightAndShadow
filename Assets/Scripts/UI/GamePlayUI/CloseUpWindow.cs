using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class CloseUpWindow : SingleTargetWindow
    {
        public static CloseUpWindow Instance;
        private Entity CloseUpTarget = Entity.Null;


        [Header("Custom Config")] public GameObject closeUpPanel;
        public Camera closeUpCamera;
        public RawImage closeUpImage;
        public Slider closeUpStatSlider;
        public Vector3 camBias;
        public LayerMask closeUpLayerMask;

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
            _closeUpTargetColliderSize = _em.GetComponentData<InteractableAttr>(target).BoxColliderSize;
            CloseUpTarget = target;
            _oriLayer = SetLayerRecursively(CloseUpTarget, _closeUpLayerIndex);

            // var buffer = _em.GetBuffer<LinkedEntityGroup>(_closeUpTarget);
            // var s = _em.GetSharedComponent<RenderFilterSettings>(buffer[1].Value);
            // s.Layer = closeUpLayerMask;
            // _em.SetSharedComponent(buffer[1].Value, s);
            return true;
        }

        public override bool HasTarget()
        {
            return CloseUpTarget != Entity.Null;
        }

        private int _closeUpLayerIndex;
        private int _oriLayer;
        private Vector3 _cameraBias;
        private Vector3 _closeUpTargetColliderSize = Vector3.zero;
        private EntityManager _em;
        private EntityQuery _notPauseTag;


        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
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