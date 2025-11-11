using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Collections;

public class KHS_Script_UIImgFunc : MonoBehaviour
{
    public static event Action<bool> RoundUIEvt;

    [Header("Round 이미지 연출 관련")]
    [Tooltip("Round 이미지 오브젝트 연결")]
    [SerializeField]
    private GameObject[] roundImgGos;
    private Material[] images;

    [Header("연출 설정")]
    [SerializeField, Tooltip("열리는 애니메이션(0->1) 시간(초)")]
    private float openDuration = 1.0f;
    [SerializeField, Tooltip("닫히는 애니메이션(1->0) 시간(초)")]
    private float closeDuration = 0.6f;
    [SerializeField, Tooltip("완전히 열린 후 대기 시간(초)")]
    private float holdDuration = 2.0f;

    void Start()
    {
        List<GameObject> childObjects = new List<GameObject>();
        List<Material> ImagesMaterials = new List<Material>();

        foreach (Transform child in transform)
        {
            childObjects.Add(child.gameObject);
            ImagesMaterials.Add(child.GetComponent<Image>().material);
            child.gameObject.SetActive(false);
        }

        images = ImagesMaterials.ToArray();
        roundImgGos = childObjects.ToArray();
        
        foreach(Material mat in images)
        {
            mat.SetFloat("_Progress", 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartRoundFunc(int _round)
    {
        if (_round < 0 || _round >= roundImgGos.Length)
        {
            Debug.LogError($"[UIImgFunc] 잘못된 round 인덱스 {_round}");
            return;
        }

        StartCoroutine(StartRoundCoroutine(_round));
    }

    private IEnumerator StartRoundCoroutine(int _round)
    {
        RoundUIEvt?.Invoke(true);
        Time.timeScale = 0f;

        roundImgGos[_round].SetActive(true);
        Material mat = images[_round];
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime; // UI 연출은 unscaled로 하는게 좋음 (일시정지에도 보이길 원하면)
            float t = Mathf.Clamp01(elapsed / openDuration);
            float progress = Mathf.SmoothStep(0f, 1f, t);
            if (mat.HasProperty("_Progress"))
                mat.SetFloat("_Progress", progress);
            yield return null;
        }

        // 강제로 1로 맞추기
        if (mat.HasProperty("_Progress"))
            mat.SetFloat("_Progress", 1f);

        // 열려있는 동안 대기
        yield return new WaitForSecondsRealtime(holdDuration);

        // 닫기 코루틴 실행
        yield return StartCoroutine(StartCloseCoroutine(_round));

        Time.timeScale = 1f;
    }
    private IEnumerator StartCloseCoroutine(int _round)
    {
        roundImgGos[_round].SetActive(true);
        Material mat = images[_round];

        // 닫기 (1 -> 0)
        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            float progress = Mathf.SmoothStep(1f, 0f, t);
            if (mat.HasProperty("_Progress"))
                mat.SetFloat("_Progress", progress);
            yield return null;
        }

        // 강제로 0으로 맞추기
        if (mat.HasProperty("_Progress"))
            mat.SetFloat("_Progress", 0f);

        // 비활성화
        roundImgGos[_round].SetActive(false);
        RoundUIEvt?.Invoke(false);
        yield break;
    }
}
