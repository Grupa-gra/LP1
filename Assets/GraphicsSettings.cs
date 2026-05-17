using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GraphicsSettings : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vSyncToggle;

    private Resolution[] resolutions;
    private List<Resolution> uniqueResolutions = new List<Resolution>();

    private void Start()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        foreach (var resolution in resolutions)
        {
            bool isDuplicate = false;

            foreach (var uniqueResolution in uniqueResolutions)
            {
                if (uniqueResolution.width == resolution.width &&
                    uniqueResolution.height == resolution.height)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                uniqueResolutions.Add(resolution);

                string option = resolution.width + " x " + resolution.height;
                options.Add(option);

                if (resolution.width == 1920 && resolution.height == 1080)
                {
                    currentResolutionIndex = options.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);

        SetResolution(1920, 1080);

        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
        vSyncToggle.onValueChanged.AddListener(OnVSyncToggleChanged);
    }

    public void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, Screen.fullScreenMode);

        float targetAspect = (float)width / height;
        float currentAspect = (float)Screen.width / Screen.height;

        if (Mathf.Abs(targetAspect - currentAspect) > 0.01f)
        {
            Debug.LogWarning("Uwaga: Proporcje mogą być niedopasowane. Mogą pojawić się paski.");
        }
    }

    private void OnResolutionChanged(int resolutionIndex)
    {
        Resolution selectedResolution = uniqueResolutions[resolutionIndex];
        SetResolution(selectedResolution.width, selectedResolution.height);
    }

    private void OnFullscreenToggleChanged(bool isFullscreen)
    {
        SetFullscreen(isFullscreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreenMode = isFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.fullScreen = isFullscreen;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    private void OnVSyncToggleChanged(bool isVSyncOn)
    {
        QualitySettings.vSyncCount = isVSyncOn ? 1 : 0;
    }
}