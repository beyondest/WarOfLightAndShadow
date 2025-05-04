using SparFlame.BootStrapper;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Command;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SparFlame.UI.GamePlay
{
    public class CursorControllerNew : MonoBehaviour
    {
        [Header("Config")] [SerializeField]
        private Texture2D normalMouseTexture;
        [SerializeField] private Texture2D clickedCursorTexture;

        [SerializeField] private bool useSoftwareCursor;

        [SerializeField] private Image cursorLeftImage;
        [SerializeField] private Vector3 cursorLeftOffset;

        [SerializeField] private float screenXMargin;
        [SerializeField] private float screenYMargin;
        
        // Internal Data

        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;
        
        
        private RectTransform _cursorLeftRectTransform;
        private Quaternion _cursorLeftRotation;

        private EntityManager _em;
        private EntityQuery _gamingTag;
        private EntityQuery _cursorData;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            SetCursor(normalMouseTexture);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(GamingTag));
            _cursorData = _em.CreateEntityQuery(typeof(CursorData));

            _cursorLeftRectTransform = cursorLeftImage.rectTransform;
            _cursorLeftRotation = cursorLeftImage.rectTransform.rotation;
            cursorLeftImage.enabled = false;
            _minX = screenXMargin;
            _maxX = Screen.width - screenXMargin;
            _minY = screenYMargin;
            _maxY = Screen.height - screenYMargin;
        }

        private void Update()
        {
            if (!HandleFocus()) return;

            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                SetCursor(clickedCursorTexture);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                SetCursor(normalMouseTexture);
            }

            // When not in gaming status, do nothing
            if (_gamingTag.IsEmpty )
            {
                return;
            }

            var newPos = Input.mousePosition + cursorLeftOffset;
            newPos.x = Mathf.Clamp(newPos.x,_minX, _maxX );
            newPos.y = Mathf.Clamp(newPos.y, _minY, _maxY);
            _cursorLeftRectTransform.position = newPos;
            _cursorLeftRectTransform.rotation = _cursorLeftRotation;

            var data = _cursorData.GetSingleton<CursorData>();
            if (data.LeftCursorType is CursorType.ArrowDown or CursorType.ArrowUp or CursorType.ArrowLeft
                or CursorType.ArrowRight or CursorType.ArrowLeftDown or CursorType.ArrowLeftUp
                or CursorType.ArrowRightDown or CursorType.ArrowRightUp or CursorType.Drag)
            {
                Cursor.visible = false;
                cursorLeftImage.enabled = true;
                cursorLeftImage.sprite = BasicUIResourceManager.Instance.CursorSprites[data.LeftCursorType];
            }
            else
            {
                cursorLeftImage.enabled = false;
            }
        }


        private static bool HandleFocus()
        {
            var focus = Application.isFocused;
            Cursor.visible = focus;
            Cursor.lockState = focus ? CursorLockMode.Confined : CursorLockMode.None;
            return focus;
        }


        Texture2D ScaleTexture(Texture2D source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private void SetCursor(Texture2D texture2D)
        {
            Cursor.SetCursor(texture2D, new Vector2(x: texture2D.width / 2f, y: texture2D.height / 2f),
                useSoftwareCursor ? CursorMode.ForceSoftware : CursorMode.Auto);
        }
    }
}