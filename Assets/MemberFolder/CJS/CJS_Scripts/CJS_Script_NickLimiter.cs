using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class CJS_Script_NickLimiter : MonoBehaviour
{
    public bool allowDigits = false;   // 숫자 허용하려면 체크
    public bool autoUppercase = true;  // 자동 대문자 변환

    TMP_InputField input;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();
        input.characterLimit = 8;                  //  8글자 하드 제한
        input.onValidateInput += ValidateChar;     //  실시간 필터
    }

    char ValidateChar(string text, int charIndex, char added)
    {
        if (text.Length >= 8) return '\0';
        char c = autoUppercase ? char.ToUpperInvariant(added) : added;

        if (char.IsLetterOrDigit(c)) return c;       // 영문 대문자만
        return '\0';                               // 그 외 차단
    }
}
