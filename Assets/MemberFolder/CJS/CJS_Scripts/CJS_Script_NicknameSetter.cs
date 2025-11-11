using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CJS_Script_NicknameSetter : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField input;
    public CJS_Script_PinballRankingService service;

    [Header("Options")]
    [Tooltip("서비스가 전혀 없을 때 디버그 편의용으로 자동 생성합니다. (Instance가 알아서 생성)")]
    public bool autoCreateServiceIfMissing = true;

    void Awake()
    {
        if (!input) input = GetComponentInChildren<TMP_InputField>(true);

        // 즉시 한번
        TryWireService();

        // 씬 바뀐 뒤(중복 삭제 타이밍 후) 한 프레임 쉬고 다시 연결
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 서비스 준비 알림이 오면 즉시 연결
        CJS_Script_PinballRankingService.InstanceReady += OnServiceReady;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CJS_Script_PinballRankingService.InstanceReady -= OnServiceReady;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 중복 프리팹이 파괴된 다음 프레임에 확실히 살아있는 싱글톤을 잡는다
        StartCoroutine(CoRewireNextFrame());
    }

    private void OnServiceReady(CJS_Script_PinballRankingService srv)
    {
        service = srv;
    }

    private IEnumerator CoRewireNextFrame()
    {
        yield return null; // 1프레임 대기 (중복 프리팹 파괴 완료 타이밍 보장)
        TryWireService();
    }

    private void TryWireService()
    {
        // 파괴되었거나 비어 있으면 싱글톤 확보 (없으면 생성)
        if (!service || service.Equals(null))
        {
            var inst = CJS_Script_PinballRankingService.Instance; // EnsureInstance 호출됨
            if (inst) service = inst;
        }
    }

    public void OnClickSet()
    {
        TryWireService();
        if (!service)
        {
            Debug.LogError("[NicknameSetter] RankingService not found.");
            return;
        }

        var nick = (input && !string.IsNullOrWhiteSpace(input.text))
                   ? input.text.Trim()
                   : "Guest";

        service.SetNicknameAndStart(nick);
        Debug.Log($"[NicknameSetter] Nickname = {service.Nickname}");
    }
}
