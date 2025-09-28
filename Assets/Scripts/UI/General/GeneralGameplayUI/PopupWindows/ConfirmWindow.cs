using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class ConfirmWindow : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        // Interface
        public static ConfirmWindow Instance { get; private set; }
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private RectTransform confirmButtonTransform;
        [SerializeField] private RectTransform singleConfirmLayoutTransform;
        [SerializeField] private RectTransform withCancelLayoutTransform;

        public void Show(string message, Action onConfirm = null, Action onCancel = null, bool showCancelButton = true)
        {
            GeneralModalWindowController.Instance.Show();
            messageText.text = message;
            panel.SetActive(true);
            confirmButton.image.rectTransform.anchoredPosition = showCancelButton
                ? withCancelLayoutTransform.anchoredPosition
                : singleConfirmLayoutTransform.anchoredPosition;
            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.gameObject.SetActive(showCancelButton);
            confirmButton.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
                Hide();
            });
            cancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Hide();
            });
        }

        public void Hide()
        {
            GeneralModalWindowController.Instance.Hide();
            panel.SetActive(false);
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            Hide();
        }
    }
}