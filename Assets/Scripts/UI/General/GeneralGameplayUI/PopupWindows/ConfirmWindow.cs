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
        
        public void Show(string message, Action onConfirm = null, Action onCancel = null)
        {
            messageText.text = message;
            panel.SetActive(true);
            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            
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
            panel.SetActive(false);
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }

        private void Awake()
        {
            if(!Instance)
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