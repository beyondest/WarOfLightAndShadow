// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
//
// public class ResolutionSettingsTMP : MonoBehaviour
// {
//     public TMP_Dropdown resolutionDropdown;
//     public Toggle fullscreenToggle;
//
//     private Resolution[] resolutions;
//     private int currentResolutionIndex;
//
//     void Start()
//     {
//         resolutions = Screen.resolutions;
//         resolutionDropdown.ClearOptions();
//
//         var options = new System.Collections.Generic.List<string>();
//         currentResolutionIndex = 0;
//
//         for (int i = 0; i < resolutions.Length; i++)
//         {
//             Resolution res = resolutions[i];
//             string option = $"{res.width} x {res.height} @ {res.refreshRate}Hz";
//             options.Add(option);
//
//             if (res.width == Screen.currentResolution.width &&
//                 res.height == Screen.currentResolution.height &&
//                 res.refreshRate == Screen.currentResolution.refreshRate)
//             {
//                 currentResolutionIndex = i;
//             }
//         }
//
//         resolutionDropdown.AddOptions(options);
//         resolutionDropdown.value = currentResolutionIndex;
//         resolutionDropdown.RefreshShownValue();
//
//         fullscreenToggle.isOn = Screen.fullScreen;
//
//         resolutionDropdown.onValueChanged.AddListener(OnResolutionChange);
//         fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
//     }
//
//     void OnResolutionChange(int index)
//     {
//         ApplyResolution(index, fullscreenToggle.isOn);
//     }
//
//     void OnFullscreenToggle(bool isFullscreen)
//     {
//         ApplyResolution(resolutionDropdown.value, isFullscreen);
//     }
//
//     void ApplyResolution(int index, bool isFullscreen)
//     {
//         Resolution res = resolutions[index];
//         Screen.SetResolution(res.width, res.height, isFullscreen, res.refreshRate);
//     }
// }

using SparFlame.UI.SubGameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FullscreenToggleOnly : MonoBehaviour
{
    public Toggle fullscreenToggle;
    public Toggle muteToggle;
    public Toggle hintToggle;
    void Start()
    {
        SetFullscreen(true);
        HintsEnable(true);

        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        
        muteToggle.isOn = AudioListener.volume == 0f;
        muteToggle.onValueChanged.AddListener(Mute);
        
        hintToggle.isOn = UpRightButtonWindow.Instance.hintsPopupWindow.activeSelf;
        hintToggle.onValueChanged.AddListener(HintsEnable);
    }

    void SetFullscreen(bool isFullscreen)
    {
        Screen.SetResolution(1920, 1080, isFullscreen);
    }
    void Mute(bool isMuted)
    {
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    public void HintsEnable(bool enable)
    {
        UpRightButtonWindow.Instance.hintsPopupWindow.SetActive(enable);
    }
}
