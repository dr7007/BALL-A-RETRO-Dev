using System.Collections;
using TMPro;
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
        originalReadOnly = inputField.readOnly;
        inputField.readOnly = true;
        refresh = StartCoroutine(RefreshIdentity());
    }

    private void OnDisable()
    {
        if (refresh == null) return;
        StopCoroutine(refresh);
        refresh = null;
        if (inputField != null) inputField.readOnly = originalReadOnly;
    }

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
        if (!loggedIn && inputField.placeholder is TMP_Text placeholder)
            placeholder.text = "Steam을 실행하고 로그인해주세요";
    }
}
