using PSH;
using System;
using System.Collections;
using UnityEngine;

public class KHS_Script_CameraManager : MonoBehaviour
{
    public static event Action<Camera> CameraChangeEvt;
    public static event Action<bool> MonitorEvt;
    public static event Action RoundStartEvt;

    public GameObject[] cameraGos;

    [SerializeField] 
    private float moveDuration = 2f; // 이동 시간

    [SerializeField]
    private KHS_Script_ScoreManager scoreManager;


    private Vector3 cameraInitPos = Vector3.zero;
    private Quaternion cameraInitRot = Quaternion.identity;
    private Vector3 cameraTargetPos = Vector3.zero;
    private Quaternion cameraTargetRot = Quaternion.identity;
    private Coroutine moveCoroutine;
    private bool isMain = true;

    private void Start()
    {
        MainCamOn();
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
        cameraInitPos = cameraGos[0].transform.position;
        cameraInitRot = cameraGos[0].transform.rotation;
        cameraTargetPos = cameraGos[2].transform.position;
        cameraTargetRot = cameraGos[2].transform.rotation;
    }
    private void Update()
    {

    }

    private void OnEnable()
    {
        KHS_Script_PortalController.portalEvt += SubCamOn;
        KHS_Script_PlincoFunction.ReturnPortalEvt += MainCamOn;
        PSH_Script_GameSceneDirector.OpeningEyeEvt += MonitorOnBeforeDialogue;
        PSH_Script_DialogueUI.DialogueEvt += MonitorOffAfterDialogue;
    }
    private void OnDisable()
    {
        KHS_Script_PortalController.portalEvt -= SubCamOn;
        KHS_Script_PlincoFunction.ReturnPortalEvt -= MainCamOn;
        PSH_Script_GameSceneDirector.OpeningEyeEvt -= MonitorOnBeforeDialogue;
        PSH_Script_DialogueUI.DialogueEvt -= MonitorOffAfterDialogue;
    }

    public void MainCamOn()
    {
        Debug.LogError($"CameraReturnMain [MainCamOn]");
        CameraChangeEvt.Invoke(cameraGos[0].GetComponent<Camera>());
        cameraGos[0].SetActive(true);
        cameraGos[1].SetActive(false);
        cameraGos[3].SetActive(false);
    }
    public void SubCamOn(int _idx)
    {
        Debug.LogError($"CameraChange : ({_idx}) [SubCamOn]");
        CameraChangeEvt.Invoke(cameraGos[_idx].GetComponent<Camera>());
        cameraGos[0].SetActive(false);
        cameraGos[_idx].SetActive(true);
    }

    public void MonitorOn(PSH_Script_DialogueUI _DialogueUI = null)
    {
        MonitorEvt?.Invoke(true);

        // cameraGos[2]를 활성화하고 이동 시작점(cameraGos[0] 위치)으로 설정
        cameraGos[2].SetActive(true);
        cameraGos[2].transform.position = cameraInitPos;
        cameraGos[2].transform.rotation = cameraInitRot;

        // 기존 코루틴이 돌고 있다면 중단
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        // 부드럽게 이동 코루틴 시작
        moveCoroutine = StartCoroutine(MoveCameraSmooth(cameraGos[2].transform, cameraTargetPos, cameraTargetRot, moveDuration, () => _DialogueUI?.Play("Intro")));

        // cameraGos[0]은 비활성화
        cameraGos[0].SetActive(false);
    }
    public void MonitorOff()
    {

        // cameraGos[0]를 활성화하고 이동 시작점(cameraGos[2] 위치)으로 설정
        cameraGos[0].SetActive(true);
        cameraGos[0].transform.position = cameraTargetPos;
        cameraGos[0].transform.rotation = cameraTargetRot;

        // 기존 코루틴이 돌고 있다면 중단
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        // 부드럽게 이동 코루틴 시작
        moveCoroutine = StartCoroutine(MoveCameraSmooth(cameraGos[0].transform, cameraInitPos, cameraInitRot, moveDuration, OnCameraReturnComplete));

        // cameraGos[0]은 비활성화
        cameraGos[2].SetActive(false);
        MonitorEvt?.Invoke(false);

    }
    public void MonitorOffAfterDialogue(string _str)
    {
        if(_str == "Intro")
        {
            MonitorOff();
        }
    }

    public void MonitorOnButton()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        MonitorOn(null);
    }
    
    public void MonitorOnBeforeDialogue(PSH_Script_DialogueUI _dialogueUI)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        MonitorOn(_dialogueUI);
    }

    private void OnCameraReturnComplete()
    {
        StartCoroutine(PlayRoundAndStartGame());
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private IEnumerator PlayRoundAndStartGame()
    {
        Debug.Log("[Round] 표시 시작");

        // Round 이미지 연출 호출 (UI 매니저에서 처리 중이라면 여기에 연결)
        var roundUI = FindAnyObjectByType<KHS_Script_UIImgFunc>();
        if (roundUI != null)
        {
            roundUI.StartRoundFunc(scoreManager.RoundRespone() - 1);
            Debug.Log("[Round] 라운드 연출 실행 중...");
        }

        // 연출 시간 대기 (필요 시 roundUI에서 대기시간 가져오기)
        yield return new WaitForSeconds(2f);

        Debug.Log("[Round] 표시 완료, 게임 시작!");
        RoundStartEvt?.Invoke(); // 게임 로직 쪽에서 이 이벤트 받아서 시작 처리
    }

    private IEnumerator MoveCameraSmooth(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration, Action onComplete = null)
    {
        Debug.Log("[CameraTransition] MoveCameraSmooth 시작");
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration); // 부드러운 곡선 보간
            cam.position = Vector3.Lerp(startPos, targetPos, t);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, t); // 회전도 부드럽게 보간
            yield return null;
        }

        cam.position = targetPos; // 정확히 목표 위치에 맞추기
        cam.rotation = targetRot;
        moveCoroutine = null;

        Debug.Log("[CameraTransition] MoveCameraSmooth 완료");
        
        onComplete?.Invoke();
    }
}
