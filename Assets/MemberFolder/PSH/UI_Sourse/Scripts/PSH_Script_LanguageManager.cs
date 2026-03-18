using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 필수
using UnityEngine.Localization.Settings; // Localization 패키지 필수

public class PSH_Script_LanguageManager : MonoBehaviour
{
    [Tooltip("언어를 변경할 드롭다운 UI를 연결하세요")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Start()
    {
        // 시스템 초기화가 끝날 때까지 기다렸다가 드롭다운 세팅을 시작합니다.
        StartCoroutine(InitDropdownCoroutine());
    }

    private IEnumerator InitDropdownCoroutine()
    {
        // 1. 유니티 Localization 시스템이 준비될 때까지 대기
        yield return LocalizationSettings.InitializationOperation;

        // 2. 드롭다운 안에 있는 기존 옵션들(Item) 다 지우기
        languageDropdown.options.Clear();

        // 3. 유니티 설정에 있는 언어(한국어, 영어)를 가져와서 드롭다운에 자동 추가
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var options = new List<TMP_Dropdown.OptionData>();
        int currentLanguageIndex = 0;

        for (int i = 0; i < locales.Count; ++i)
        {
            var locale = locales[i];
            
            // "Korean (ko)", "English (en)" 같은 이름을 드롭다운 메뉴로 만듦
            options.Add(new TMP_Dropdown.OptionData(locale.name)); 

            // 현재 시스템에 적용된 언어와 일치하면 해당 번호를 기억해둠
            if (LocalizationSettings.SelectedLocale == locale)
            {
                currentLanguageIndex = i;
            }
        }

        // 4. 세팅한 옵션들을 드롭다운에 넣고, 현재 언어로 값 맞춰주기
        languageDropdown.options = options;
        languageDropdown.value = currentLanguageIndex;

        // 5. 유저가 드롭다운을 클릭해서 바꿀 때마다 OnLanguageChanged 함수가 실행되도록 연결
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    // 드롭다운 값이 바뀔 때마다 호출되는 함수
    private void OnLanguageChanged(int index)
    {
        // 선택한 번호에 맞는 언어로 유니티 시스템 전체 언어 변경!
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
    }
}