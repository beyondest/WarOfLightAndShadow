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
        [SerializeField] private Image cursorLeftImage; 
        [SerializeField] private Image cursorRightImage;
        [SerializeField] private Vector3 cursorLeftOffset;
        [SerializeField] private Vector3 cursorRightOffset;
        private RectTransform _cursorLeftRectTransform;
        private RectTransform _cursorRightRectTransform;
        
        [Tooltip("Asset/Resources/UI/Cursor/CursorAttack.png, then this should be set to UI/Cursor. Notice that all the cursors in that path should be Cursor + Enum(CursorType) name")]
        [SerializeField] private string cursorSpritePath = "UI/Cursor";
        
        
        private Dictionary<CursorType, Sprite> _cursorDictionary;
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _cursorData;
        private Quaternion _cursorLeftRotation;
        private Quaternion _cursorRightRotation;
        
        private void Awake()
        {
            _cursorDictionary = new Dictionary<CursorType, Sprite>();
            // LoadCursorSprites();
            _cursorDictionary = CR.ResourceLoadTypeSprites<CursorType>(cursorSpritePath, prefix: "Cursor");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnEnable()
        {
            _cursorLeftRectTransform = cursorLeftImage.rectTransform;
            _cursorRightRectTransform = cursorRightImage.rectTransform;
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _cursorData = _em.CreateEntityQuery(typeof(CursorData));
            SetDefaultCursor();
            _cursorLeftRotation = cursorLeftImage.rectTransform.rotation;
            _cursorRightRotation = cursorRightImage.rectTransform.rotation;
        }

        private void Update()
        {
            HandleFocus();
            _cursorLeftRectTransform.position = Input.mousePosition + cursorLeftOffset; // 让 UI 鼠标跟随鼠标
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
            cursorLeftImage.sprite = _cursorDictionary[cursorManageData.LeftCursorType];
            cursorRightImage.sprite = _cursorDictionary[cursorManageData.RightCursorType];

        }

        private void SetDefaultCursor()
        {
            cursorLeftImage.sprite = _cursorDictionary[CursorType.UI];
            cursorRightImage.sprite = _cursorDictionary[CursorType.None];
        }
        private static void HandleFocus()
        {
            Cursor.lockState = Application.isFocused ? CursorLockMode.Confined : CursorLockMode.None;
        }


    }
}