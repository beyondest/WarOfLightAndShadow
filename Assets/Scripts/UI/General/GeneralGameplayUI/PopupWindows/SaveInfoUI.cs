using System.Collections;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class SaveInfoUI : MonoBehaviour
    {
        [SerializeField] private UIFadeInOut textFade;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float textStayDuration = 2f;
        
        [Header("Save Spin Circle")]
        [SerializeField] private Image circle;
        [SerializeField] private float circleSpinSpeed;
        private Coroutine _spinCoroutine;
        
        private void Start()
        {
            circle.gameObject.SetActive(false);
            
            SaveLoadController.Instance.OnStartSave += SaveStart;
            SaveLoadController.Instance.OnSaveComplete += SaveComplete;
        }



        private void SaveStart(SaveType saveType)
        {
            StartSpin();
        }

        private void StartSpin()
        {
            circle.gameObject.SetActive(true);
            if (_spinCoroutine != null)
                StopCoroutine(_spinCoroutine);
            _spinCoroutine = StartCoroutine(SpinCoroutine());
        }

        private void StopSpin()
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
            circle.gameObject.SetActive(false);
        }
        private void SaveComplete(SaveType saveType)
        {
            StopSpin();
            switch (saveType)
            {
                case SaveType.Manual:
                    text.text = "Manul Save Complete!";
                    textFade.FadeInThenFadeOut(textStayDuration);
                    break;
                case SaveType.Automatic:
                    text.text = "Automatic Save Complete!";
                    textFade.FadeInThenFadeOut(textStayDuration);
                    break;
                case SaveType.SaveSubGameplayDataToTmp:
                case SaveType.SaveEnemySpecificArmyGroupSubData:
                    break;
                default:
                    BurstSafe.UnexpectedEnum(saveType);
                    break;
            }

        }
        
        
        private IEnumerator SpinCoroutine()
        {
            while (true)
            {
                // 匀速旋转，每帧旋转 circleSpinSpeed * deltaTime
                circle.transform.Rotate(0f, 0f, -circleSpinSpeed * Time.deltaTime);
                yield return null;
            }
            
        }
        
    }
    
}