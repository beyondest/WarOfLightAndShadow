using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Command;
using SparFlame.UI.General;

namespace SparFlame.UI.GamePlay
{

    public class CursorController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private Image cursorLeftImage; 
        [SerializeField] private Image cursorRightImage;
        [SerializeField] private Vector3 cursorLeftOffset;
        [SerializeField] private Vector3 cursorRightOffset;
        
        // Internal Data
        private RectTransform _cursorLeftRectTransform;
        private RectTransform _cursorRightRectTransform;
        private Quaternion _cursorLeftRotation;
        private Quaternion _cursorRightRotation;
        

        
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _cursorData;
    
        
        private void Awake()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _cursorData = _em.CreateEntityQuery(typeof(CursorData));
            _cursorLeftRectTransform = cursorLeftImage.rectTransform;
            _cursorRightRectTransform = cursorRightImage.rectTransform;
            _cursorLeftRotation = cursorLeftImage.rectTransform.rotation;
            _cursorRightRotation = cursorRightImage.rectTransform.rotation;
        }

        private void Update()
        {
            if(!BasicWindowResourceManager.Instance.IsResourceLoaded())return;
            HandleFocus();
            _cursorLeftRectTransform.position = Input.mousePosition + cursorLeftOffset; 
            _cursorRightRectTransform.position = Input.mousePosition + cursorRightOffset;
            _cursorLeftRectTransform.rotation = _cursorLeftRotation;
            _cursorRightRectTransform.rotation = _cursorRightRotation;
            // When game paused, set default cursor
            if (_notPauseTag.IsEmpty)
            {
                SetDefaultCursor();
                return;
            }
            var cursorManageData = _cursorData.GetSingleton<CursorData>();
            cursorLeftImage.sprite = BasicWindowResourceManager.Instance.CursorSprites[cursorManageData.LeftCursorType];
            cursorRightImage.sprite = BasicWindowResourceManager.Instance.CursorSprites[cursorManageData.RightCursorType];

        }

        private void SetDefaultCursor()
        {
            cursorLeftImage.sprite = BasicWindowResourceManager.Instance.CursorSprites[CursorType.UI];
            cursorRightImage.sprite = BasicWindowResourceManager.Instance.CursorSprites[CursorType.None];
        }
        private static void HandleFocus()
        {
            Cursor.lockState = Application.isFocused ? CursorLockMode.Confined : CursorLockMode.None;
        }


    }
}