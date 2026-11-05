using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class PSH_Script_ResolutionSettings : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    private List<Resolution> filteredResolutions;

    // 💡 시니어 포인트 1: 매직 넘버 제거. 인덱스(0, 1, 2)와 매칭되는 배열을 미리 선언하여 관리합니다.
    private readonly FullScreenMode[] screenModes = new FullScreenMode[]
    {
        FullScreenMode.ExclusiveFullScreen, // 인덱스 0: 전체화면
        FullScreenMode.FullScreenWindow,    // 인덱스 1: 테두리 없는 창모드
        FullScreenMode.Windowed             // 인덱스 2: 창모드
    };

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
        RefreshLanguage(null);
    }
    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
    private void RefreshLanguage(Locale _)
    {
        if (screenModeDropdown == null || screenModeDropdown.options.Count == 0) return;
        int selected = screenModeDropdown.value;
        screenModeDropdown.Hide();
        SetModeOptions();
        screenModeDropdown.SetValueWithoutNotify(selected);
        screenModeDropdown.RefreshShownValue();
    }
    private void SetModeOptions()
    {
        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string> {
            PSH_Script_Localization.Text("Sys_Fullscreen"),
            PSH_Script_Localization.Text("Sys_Borderless"),
            PSH_Script_Localization.Text("Sys_Windowed") });
    }

    void Start()
    {
        InitResolutionDropdown();
        InitScreenModeDropdown();
    }

    private void InitResolutionDropdown()
    {
        Resolution[] allResolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        int currentResolutionIndex = 0;
        List<string> options = new List<string>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            bool isUnique = true;
            foreach (Resolution res in filteredResolutions)
            {
                if (res.width == allResolutions[i].width && res.height == allResolutions[i].height)
                {
                    isUnique = false;
                    break;
                }
            }

            if (isUnique)
            {
                filteredResolutions.Add(allResolutions[i]);
                
                // 💡 시니어 포인트 2: '+' 연산자 대신 문자열 보간법($)을 사용하여 가비지(GC) 생성을 줄입니다.
                options.Add($"{allResolutions[i].width} x {allResolutions[i].height}");

                if (allResolutions[i].width == Screen.width && allResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private void InitScreenModeDropdown()
    {
        SetModeOptions();

        // 현재 기기의 화면 모드가 배열의 몇 번째 인덱스인지 찾아서 UI 기본값으로 세팅합니다.
        int currentModeIndex = System.Array.IndexOf(screenModes, Screen.fullScreenMode);
        
        // 예외 처리 (만약 일치하는 모드가 없으면 0번 전체화면으로 초기화)
        screenModeDropdown.SetValueWithoutNotify(currentModeIndex != -1 ? currentModeIndex : 0);
        
        screenModeDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }

    public void SetScreenMode(int modeIndex)
    {
        // 💡 시니어 포인트 3: if (mode == 0) 같은 분기문 없이, 넘어온 인덱스로 배열에서 바로 값을 꺼내 씁니다.
        if (modeIndex >= 0 && modeIndex < screenModes.Length)
        {
            FullScreenMode targetMode = screenModes[modeIndex];
            Screen.SetResolution(Screen.width, Screen.height, targetMode);
        }
    }
    
}
