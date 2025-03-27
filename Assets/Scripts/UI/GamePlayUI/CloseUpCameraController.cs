using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.Units;
using Unity.Cinemachine;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class CloseUpCameraController : MonoBehaviour
    {

        public static CloseUpCameraController Instance;
        public Camera closeUpCamera;
        public Vector3 camBias;
        public RawImage closeUpImage;
        public Slider closeUpStatSlider;

        

        
        public void ShowCloseUpWindow(Entity target)
        {
            _target = target;
            closeUpCamera.enabled = true;
            closeUpImage.enabled = true;
            closeUpStatSlider.enabled = true;
        }

        public void HideCloseUpShowWindow()
        {
            _target = Entity.Null;
            closeUpCamera.enabled = false;
            closeUpImage.enabled = false;
            closeUpStatSlider.enabled = false;
        }

        
        private EntityManager _em;
        private Entity _target = Entity.Null;
        private EntityQuery _notPauseTag;

        
        
        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            closeUpImage.texture = closeUpCamera.targetTexture;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            UpdateCloseUpShow();
        }

        
        
        private void UpdateCloseUpShow()
        {
            if (_target != Entity.Null)
            {
                var tarTransform = _em.GetComponentData<LocalTransform>(_target);
                var statData = _em.GetComponentData<StatData>(_target);
                closeUpStatSlider.value = (float)statData.CurValue/statData.MaxValue;
                closeUpCamera.transform.position = (Vector3)tarTransform.Position + camBias;
                closeUpCamera.transform.LookAt(tarTransform.Position);
            }
        }

    }
}