using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;

namespace SparFlame.UI.Cursor
{

    public class CursorManager : MonoBehaviour
    {
        [System.Serializable]
        public struct CursorSprite
        {
            public CursorType type;
            public Sprite sprite;
        }
        
        
        [SerializeField] private Image cursorLeftImage; 
        [SerializeField] private Image cursorRightImage;
        public RectTransform cursorLeftRectTransform;
        public RectTransform cursorRightRectTransform;

        [Tooltip("Asset/Resources/UI/Cursor/CursorAttack.png, then this should be set to UI/Cursor. Notice that all the cursors in that path should be Cursor + Enum(CursorType) name")]
        [SerializeField] private string cursorSpritePath = "UI/Cursor";
        
        
        private Dictionary<CursorType, Sprite> _cursorDictionary;
        private EntityManager _em;
        [SerializeField] private Vector3 cursorLeftOffset;
        [SerializeField] private Vector3 cursorRightOffset;
        private Quaternion _cursorLeftRotation;
        private Quaternion _cursorRightRotation;
        
        private void Awake()
        {
            _cursorDictionary = new Dictionary<CursorType, Sprite>();
            LoadCursorSprites();
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            SetDefaultCursor();
            _cursorLeftRotation = cursorLeftImage.rectTransform.rotation;
            _cursorRightRotation = cursorRightImage.rectTransform.rotation;
        }

        private void Update()
        {
            HandleFocus();
            cursorLeftRectTransform.position = Input.mousePosition + cursorLeftOffset; // 让 UI 鼠标跟随鼠标
            cursorRightRectTransform.position = Input.mousePosition + cursorRightOffset;
            cursorLeftRectTransform.rotation = _cursorLeftRotation;
            cursorRightRectTransform.rotation = _cursorRightRotation;
            // When game paused, set default cursor
            if (!_em.CreateEntityQuery(typeof(NotPauseTag)).TryGetSingletonEntity< NotPauseTag>(out var _))
            {
                SetDefaultCursor();
                return;
            }

            if (!_em.CreateEntityQuery(typeof(CursorManageData)).TryGetSingletonEntity< CursorManageData>(out var dataEntity)) return;
            var cursorManageData = _em.GetComponentData<CursorManageData>(dataEntity);
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
            UnityEngine.Cursor.lockState = Application.isFocused ? CursorLockMode.Confined : CursorLockMode.None;
        }
        private void LoadCursorSprites()
        {
            _cursorDictionary = new Dictionary<CursorType, Sprite>();

            foreach (CursorType type in System.Enum.GetValues(typeof(CursorType)))
            {
                string path = $"{cursorSpritePath}/Cursor{type}";  // "Asset/Resources/UI/Cursor/CursorType.png"
                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    _cursorDictionary[type] = sprite;
                }
                else
                {
                    Debug.LogError($"Cursor sprite not found: {path}");
                }
            }
        }

    }
}