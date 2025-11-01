using System;
using SparFlame.Components.General;
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

        [Header("Left soft cursor image")] [SerializeField]
        private Image cursorLeftImage;

        [SerializeField] private Vector3 cursorLeftOffset;

        [Header("Right soft cursor image")] [SerializeField]
        private Image specialCursorImage;

        [SerializeField] private Vector3 specialCursorOffset;


        [SerializeField] private float screenXMargin;
        [SerializeField] private float screenYMargin;

        // Internal Data

        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;


        private EntityManager _em;
        private EntityQuery _subGameplayCursorData;
        private EntityQuery _circleCursorData;
        private EntityQuery _mainGameplayCursorData;
        private EntityQuery _gameStatus;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            SetDefaultCurosr(normalMouseTexture);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _subGameplayCursorData = _em.CreateEntityQuery(typeof(SubGameplayCursorData));
            _circleCursorData = _em.CreateEntityQuery(typeof(CircleCursorData));
            _mainGameplayCursorData = _em.CreateEntityQuery(typeof(MainGameplayCursorData));
            _gameStatus = _em.CreateEntityQuery(typeof(GameStatusData));
            cursorLeftImage.enabled = false;
            _minX = screenXMargin;
            _maxX = Screen.width - screenXMargin;
            _minY = screenYMargin;
            _maxY = Screen.height - screenYMargin;
        }

        private void Update()
        {
            if (!HandleFocus()) return;

            // Update default cursor
            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                SetDefaultCurosr(clickedCursorTexture);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                SetDefaultCurosr(normalMouseTexture);
            }

            
            // Update right soft cursor
            specialCursorImage.fillAmount = _circleCursorData.IsEmpty
                ? 0
                : _circleCursorData.GetSingleton<CircleCursorData>().FillAmount;

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
            var gameStatus = _gameStatus.GetSingleton<GameStatusData>().Value;
            if (gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming)
            {
                cursorLeftImage.enabled = false;
                Cursor.visible = true;
                return;
            }


            if (gameStatus == GameStatus.MainGaming)
            {
                var data = _mainGameplayCursorData.GetSingleton<MainGameplayCursorData>();
                if (data.CursorType is MainGameplayCursorType.None or MainGameplayCursorType.CheckInfo)
                {
                    cursorLeftImage.enabled = false;
                    Cursor.visible = true;
                }
                else
                {
                    cursorLeftImage.enabled = true;
                    Cursor.visible = false;
                    cursorLeftImage.sprite = BasicUIResourceManager.Instance.MainGameplayCursorSprites[data.CursorType];
                }
            }
            else
            {
                var data = _subGameplayCursorData.GetSingleton<SubGameplayCursorData>();
                if (data.LeftCursorType is SubGameplayCursorType.ArrowDown or SubGameplayCursorType.ArrowUp
                    or SubGameplayCursorType.ArrowLeft
                    or SubGameplayCursorType.ArrowRight or SubGameplayCursorType.ArrowLeftDown
                    or SubGameplayCursorType.ArrowLeftUp
                    or SubGameplayCursorType.ArrowRightDown or SubGameplayCursorType.ArrowRightUp
                    or SubGameplayCursorType.Drag)
                {
                    Cursor.visible = false;
                    cursorLeftImage.enabled = true;
                    cursorLeftImage.sprite = BasicUIResourceManager.Instance.SubGameplayCursorSprites[data.LeftCursorType];
                }
                else
                {
                    Cursor.visible = true;
                    cursorLeftImage.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (_subGameplayCursorData != default)
                    _subGameplayCursorData.Dispose();
                if (_circleCursorData != default)
                    _circleCursorData.Dispose();
                if (_mainGameplayCursorData != default)
                    _mainGameplayCursorData.Dispose();
                if (_gameStatus != default)
                    _gameStatus.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static bool HandleFocus()
        {
            var focus = Application.isFocused;
            Cursor.visible = focus;
            Cursor.lockState = focus ? CursorLockMode.Confined : CursorLockMode.None;
            return focus;
        }


        private void SetDefaultCurosr(Texture2D texture2D)
        {
            Cursor.SetCursor(texture2D, new Vector2(x: texture2D.width / 2f, y: texture2D.height / 2f),
                useSoftwareCursor ? CursorMode.ForceSoftware : CursorMode.Auto);
        }
        
    }
}