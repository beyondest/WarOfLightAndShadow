using SparFlame.Components.Input;
using SparFlame.Systems.General.BasicControl;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace SparFlame.UI.General
{
    public class CursorController : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private Texture2D normalMouseTexture;
        [SerializeField] private Texture2D clickedCursorTexture;

        [SerializeField] private bool useSoftwareCursor;

        [SerializeField] private Image cursorLeftImage;
        [SerializeField] private Vector3 cursorLeftOffset;
        [SerializeField] private Image specialCursorImage;
        [SerializeField] private Vector3 specialCursorOffset;


        [SerializeField] private float screenXMargin;
        [SerializeField] private float screenYMargin;

        // Internal Data

        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;


        private EntityManager _em;
        private EntityQuery _cursorData;
        private EntityQuery _circleCursorData;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            SetCursor(normalMouseTexture);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _cursorData = _em.CreateEntityQuery(typeof(SubGameplayCursorData));
            _circleCursorData = _em.CreateEntityQuery(typeof(CircleCursorData));
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


            // Set soft cursor position
            var newPos = Input.mousePosition + cursorLeftOffset;
            newPos.x = Mathf.Clamp(newPos.x, _minX, _maxX);
            newPos.y = Mathf.Clamp(newPos.y, _minY, _maxY);
            cursorLeftImage.rectTransform.position = newPos;

            var newSpecialPos = Input.mousePosition + specialCursorOffset;
            newSpecialPos.x = Mathf.Clamp(newSpecialPos.x, _minX, _maxX);
            newSpecialPos.y = Mathf.Clamp(newSpecialPos.y, _minY, _maxY);
            specialCursorImage.rectTransform.position = newSpecialPos;

            // Update soft cursor image
            var data = _cursorData.IsEmpty
                ? new SubGameplayCursorData
                    { LeftCursorType = SubGameplayCursorType.None, RightCursorType = SubGameplayCursorType.None }
                : _cursorData.GetSingleton<SubGameplayCursorData>();
            if (data.LeftCursorType is SubGameplayCursorType.ArrowDown or SubGameplayCursorType.ArrowUp
                or SubGameplayCursorType.ArrowLeft
                or SubGameplayCursorType.ArrowRight or SubGameplayCursorType.ArrowLeftDown
                or SubGameplayCursorType.ArrowLeftUp
                or SubGameplayCursorType.ArrowRightDown or SubGameplayCursorType.ArrowRightUp
                or SubGameplayCursorType.Drag)
            {
                Cursor.visible = false;
                cursorLeftImage.enabled = true;
                cursorLeftImage.sprite = BasicUIResourceManager.Instance.CursorSprites[data.LeftCursorType];
            }
            else
            {
                cursorLeftImage.enabled = false;
            }

            specialCursorImage.fillAmount = _circleCursorData.IsEmpty
                ? 0
                : _circleCursorData.GetSingleton<CircleCursorData>().FillAmount;
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