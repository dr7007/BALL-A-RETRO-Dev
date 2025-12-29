using UnityEngine;
using UnityEngine.UI;

public class PSH_Script_SettingInitUI : MonoBehaviour 
{
    public Slider lobbySlider, gameSlider, sfxSlider;
    public Dropdown resDropdown;

    void OnEnable() // 옵션창이 켜질 때마다 실행
    {
        lobbySlider.value = PlayerPrefs.GetFloat("Settings_Lobby_BGM", 0.6f);
        gameSlider.value = PlayerPrefs.GetFloat("Settings_Game_BGM", 0.6f);
        sfxSlider.value = PlayerPrefs.GetFloat("Settings_SFX_Vol", 1.0f);
        resDropdown.value = PlayerPrefs.GetInt("Settings_Res_Index", 0);
    }
}