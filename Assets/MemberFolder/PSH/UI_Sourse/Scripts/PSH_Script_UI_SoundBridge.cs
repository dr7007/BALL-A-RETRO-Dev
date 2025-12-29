using UnityEngine;

public class PSH_Script_UI_SoundBridge : MonoBehaviour
{
    [Header("Settings Keys")]
    private const string BGM_VOL_KEY = "Settings_BGM_Vol";
    private const string SFX_VOL_KEY = "Settings_SFX_Vol";
    private const string VFX_ON_KEY = "Settings_VFX_On";
    private const string RES_INDEX_KEY = "Settings_Res_Index";

    public static bool IsVfxEnabled { get; private set; } = true;
    private const string LOBBY_BGM_KEY = "Settings_Lobby_BGM";
    private const string GAME_BGM_KEY = "Settings_Game_BGM";

    void Awake()
    {
        // 씬 시작 시 저장된 값을 자동으로 불러와서 적용
        LoadAndApplySettings();
    }

    // 로비 BGM 슬라이더 연결용
    public void SetLobbyBGM(float vol) {
        PlayerPrefs.SetFloat(LOBBY_BGM_KEY, vol);
        if (CJS_Script_LobbyBGM.I != null) CJS_Script_LobbyBGM.I.SetVolume(vol);
    }

    // 게임 BGM 슬라이더 연결용
    public void SetGameBGM(float vol) {
        PlayerPrefs.SetFloat(GAME_BGM_KEY, vol);
        if (CJS_Script_AudioDirector.I != null) {
            CJS_Script_AudioDirector.I.bgmVolume = vol;
            if (CJS_Script_AudioDirector.I.bgmSource != null) CJS_Script_AudioDirector.I.bgmSource.volume = vol;
        }
    }
    #region [Sound Settings]
    public void SetBGMVolume(float vol)
    {
        PlayerPrefs.SetFloat(BGM_VOL_KEY, vol);

        // 로비 매니저 체크
        if (CJS_Script_LobbyBGM.I != null)
            CJS_Script_LobbyBGM.I.SetVolume(vol);

        // 게임 매니저 체크
        if (CJS_Script_AudioDirector.I != null)
        {
            CJS_Script_AudioDirector.I.bgmVolume = vol;
            if (CJS_Script_AudioDirector.I.bgmSource != null)
                CJS_Script_AudioDirector.I.bgmSource.volume = vol;
        }
    }

    public void SetSFXVolume(float vol)
    {
        PlayerPrefs.SetFloat(SFX_VOL_KEY, vol);

        if (CJS_Script_AudioDirector.I != null)
        {
            CJS_Script_AudioDirector.I.sfxVolume = vol;
            if (CJS_Script_AudioDirector.I.sfxSource != null)
                CJS_Script_AudioDirector.I.sfxSource.volume = vol;
            if (CJS_Script_AudioDirector.I.sfxLoopSource != null)
                CJS_Script_AudioDirector.I.sfxLoopSource.volume = vol * 0.7f;
        }
    }
    #endregion

    #region [VFX & Resolution]
    public void ToggleVFX(bool isOn)
    {
        IsVfxEnabled = isOn;
        PlayerPrefs.SetInt(VFX_ON_KEY, isOn ? 1 : 0);
    }

    public void SetResolution(int index)
    {
        PlayerPrefs.SetInt(RES_INDEX_KEY, index);
        switch (index)
        {
            case 0: Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow); break;
            case 1: Screen.SetResolution(1280, 720, FullScreenMode.Windowed); break;
            case 2: Screen.SetResolution(854, 480, FullScreenMode.Windowed); break;
        }
    }
    #endregion

    private void LoadAndApplySettings()
    {
        SetBGMVolume(PlayerPrefs.GetFloat(BGM_VOL_KEY, 0.6f));
        SetSFXVolume(PlayerPrefs.GetFloat(SFX_VOL_KEY, 1.0f));
        IsVfxEnabled = PlayerPrefs.GetInt(VFX_ON_KEY, 1) == 1;
        
        // 해상도는 시작할 때 자동 적용하고 싶다면 주석 해제
        // SetResolution(PlayerPrefs.GetInt(RES_INDEX_KEY, 0));
    }
}