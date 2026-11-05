using System.Collections;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;

public class KHS_Script_SteamNickLoader : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;
    private Coroutine refresh;
    private bool originalReadOnly;

    private void Awake()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        if (!BallARetroPlatform.IsSteamBuild || inputField == null) return;
        LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
        originalReadOnly = inputField.readOnly;
        inputField.readOnly = true;
        refresh = StartCoroutine(RefreshIdentity());
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
        if (refresh == null) return;
        StopCoroutine(refresh);
        refresh = null;
        if (inputField != null) inputField.readOnly = originalReadOnly;
    }

    private void RefreshLanguage(Locale _) => FillInputFieldToSteamNickname();

    private IEnumerator RefreshIdentity()
    {
        while (true)
        {
            FillInputFieldToSteamNickname();
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    public void FillInputFieldToSteamNickname()
    {
        if (!BallARetroPlatform.IsSteamBuild || inputField == null) return;
        bool loggedIn = SteamManager.TryGetLoggedInPlayer(out string nickname, out _);
        if (inputField.text != nickname) inputField.SetTextWithoutNotify(nickname);
        if (inputField.placeholder is TMP_Text placeholder)
        {
            string key = loggedIn ? "Nickname_Placeholder" : "Nickname_Steam";
            var binding = placeholder.GetComponent<PSH_Script_LocalizedText>();
            if (binding != null) binding.SetKey(key);
            else placeholder.text = PSH_Script_Localization.Text(key);
        }
    }
}
