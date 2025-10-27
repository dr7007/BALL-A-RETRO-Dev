using TMPro;
using UnityEngine;

public class CJS_Script_NicknameSetter : MonoBehaviour
{
    public TMP_InputField input;
    public CJS_Script_PinballRankingService service;

    public void OnClickSet()
    {
        var nick = string.IsNullOrWhiteSpace(input.text) ? "Guest" : input.text.Trim();
        service.SetNicknameAndStart(nick);
        Debug.Log($"Nickname = {service.Nickname}");
    }
}
