using UnityEngine;

public class PSH_Script_UI_SoundBridge : MonoBehaviour
{
    [Header("Settings Keys")]
    private const string SFX_VOL_KEY = "Settings_SFX_Vol";
    private const string VFX_ON_KEY = "Settings_VFX_On";
    private const string RES_INDEX_KEY = "Settings_Res_Index";
    
    // 이 부분이 꼬여있었습니다. 슬라이더에서 쓰는 키와 로드할 때 쓰는 키를 통일합니다.
    private const string LOBBY_BGM_KEY = "Settings_Lobby_BGM";
    private const string GAME_BGM_KEY = "Settings_Game_BGM";

    public static bool IsVfxEnabled { get; private set; } = true;

    void Awake()
    {
        // 씬이 시작될 때 저장된 값을 불러와서 '즉시' 적용합니다.
        LoadAndApplySettings();
    }

    #region [Sound Settings]

    // 로비 BGM 슬라이더 (Lobby 씬용)
    public void SetLobbyBGM(float vol) {
        PlayerPrefs.SetFloat(LOBBY_BGM_KEY, vol);
        if (CJS_Script_LobbyBGM.I != null) {
            CJS_Script_LobbyBGM.I.SetVolume(vol);
        }
    }

    // 게임 BGM 슬라이더 (Game 씬용)
    public void SetGameBGM(float vol) {
        PlayerPrefs.SetFloat(GAME_BGM_KEY, vol);
        if (CJS_Script_AudioDirector.I != null) {
            var dir = CJS_Script_AudioDirector.I;
            dir.bgmVolume = vol;
            if (dir.bgmSource != null) dir.bgmSource.volume = vol;
        }
    }

    private void LoadAndApplySettings()
    {
        float lobbyBgm = PlayerPrefs.GetFloat("Settings_Lobby_BGM", 0.6f);
        float gameBgm = PlayerPrefs.GetFloat("Settings_Game_BGM", 0.6f);
        float sfx = PlayerPrefs.GetFloat("Settings_SFX_Vol", 1.0f);

        // 로비 씬 BGM 적용
        if (CJS_Script_LobbyBGM.I != null) 
            CJS_Script_LobbyBGM.I.SetVolume(lobbyBgm);

        // 게임 씬 사운드 매니저가 있다면 (루프 소리 포함) 적용
        if (CJS_Script_AudioDirector.I != null)
        {
            var dir = CJS_Script_AudioDirector.I;
            
            // 일반 BGM
            dir.bgmVolume = gameBgm;
            if (dir.bgmSource != null) dir.bgmSource.volume = gameBgm;

            // 효과음 통합 적용 함수 호출 (이미 만들어둔 SetSFXVolume 호출)
            SetSFXVolume(sfx); 
        }

        IsVfxEnabled = PlayerPrefs.GetInt("Settings_VFX_On", 1) == 1;
    }


        #endregion

    #region [VFX & Resolution]
    public void ToggleVFX(bool isOn)
    {
        IsVfxEnabled = isOn;
        PlayerPrefs.SetInt(VFX_ON_KEY, isOn ? 1 : 0);
    }

    public void SetSFXVolume(float vol)
    {
        // 1. 값 저장
        PlayerPrefs.SetFloat("Settings_SFX_Vol", vol);

        // 2. 오디오 디렉터에게 전달
        if (CJS_Script_AudioDirector.I != null)
        {
            // 현재 저장된 BGM 볼륨과 새로운 효과음 볼륨을 동시에 보냄
            float currentGameBgm = PlayerPrefs.GetFloat("Settings_Game_BGM", 0.6f);
            CJS_Script_AudioDirector.I.SyncAllVolumes(currentGameBgm, vol);
        }
    }
    #endregion


}