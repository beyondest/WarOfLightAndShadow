using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;

namespace SparFlame.UI.General
{
    public class UIInputInteger : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private TMP_InputField inputField; // 输入框（玩家可以手动输入数值）
        [SerializeField] private Button addButton; // 加号按钮
        [SerializeField] private Button minusButton; // 减号按钮

        [Header("Settings")] [SerializeField] private int minValue = 0; // 最小值
        [SerializeField] private int maxValue = 10; // 最大值
        [SerializeField] private int initValue;
        
        [NonSerialized] public int CurrentValue;

        void Start()
        {
            CurrentValue = math.clamp(initValue, minValue, maxValue);
            UpdateInputField();

            if (addButton)
                addButton.onClick.AddListener(OnAdd);

            if (minusButton)
                minusButton.onClick.AddListener(OnMinus);

            if (inputField)
                inputField.onEndEdit.AddListener(OnInputChanged);
        }

        void OnAdd()
        {
            SetValue(CurrentValue + 1);
        }

        void OnMinus()
        {
            SetValue(CurrentValue - 1);
        }

        void OnInputChanged(string text)
        {
            if (int.TryParse(text, out int value))
            {
                SetValue(value);
            }
            else
            {
                // 输入非法时，恢复到当前值
                UpdateInputField();
            }
        }

        void SetValue(int value)
        {
            CurrentValue = Mathf.Clamp(value, minValue, maxValue);
            UpdateInputField();
        }

        void UpdateInputField()
        {
            if (inputField)
                inputField.text = CurrentValue.ToString();
        }
    }
}