using PSH;
using System;
using System.Collections;
using UnityEngine;

public class KHS_Script_CameraManager : MonoBehaviour
{
    public static event Action<Camera> CameraChangeEvt;
    public static event Action<bool> MonitorEvt;
    public static event Action RoundStartEvt;

    public static bool IsRoundCameraReady { get; private set; } = true;

    public GameObject[] cameraGos;

    [SerializeField] 
    private float moveDuration = 2f; // 이동 시간

    [SerializeField]
    private KHS_Script_ScoreManager scoreManager;

    [SerializeField]
    private CJS_Script_CameraFollowBall cameraFollow;

    private Vector3 followCameraSavedPos;
    private Quaternion followCameraSavedRot;
    private bool followCameraWasActive;

    private Vector3 cameraInitPos = Vector3.zero;
    private Quaternion cameraInitRot = Quaternion.identity;
    private Vector3 cameraTargetPos = Vector3.zero;
    private Quaternion cameraTargetRot = Quaternion.identity;
    private Coroutine moveCoroutine;
    private bool isMain = true;

    private bool isRoundCameraReturning = false;

    private bool isRoundFlow = false;
    private bool isFirstRound = true;

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
        KHS_Script_ScoreManager.Round_Clear += OnRoundClear;
        KHS_Script_UIImgFunc.RoundUIEvt += OnRoundUIEvent;

        KHS_Script_PortalController.portalEvt += SubCamOn;
        KHS_Script_PlincoFunction.ReturnPortalEvt += MainCamOn;
        PSH_Script_GameSceneDirector.OpeningEyeEvt += MonitorOnBeforeDialogue;
        PSH_Script_DialogueUI.DialogueEvt += MonitorOffAfterDialogue;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.Round_Clear -= OnRoundClear;
        KHS_Script_UIImgFunc.RoundUIEvt -= OnRoundUIEvent;

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
        // ==========================================
        // Monitor 연출 직전 Follow 상태 저장
        // ==========================================

        if (cameraFollow != null)
        {
            followCameraSavedPos =
                cameraGos[0].transform.position;

            followCameraSavedRot =
                cameraGos[0].transform.rotation;

            followCameraWasActive =
                true;

            cameraFollow.PauseFollowForMonitor();
        }

        MonitorEvt?.Invoke(true);

        // cameraGos[2]를 활성화하고 이동 시작점(cameraGos[0] 위치)으로 설정
        cameraGos[2].SetActive(true);
        // 현재 메인 카메라의 실제 위치/회전을 가져온다.
        Transform mainCamera =
            cameraGos[0].transform;

        cameraGos[2].transform.position =
            mainCamera.position;

        cameraGos[2].transform.rotation =
            mainCamera.rotation;

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
        Debug.LogError(
        $"[CAMERA 1] MonitorOnButton 호출 / " +
        $"TimeScale = {Time.timeScale}"
    );

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
        // ==========================================
        // Monitor 연출 종료
        // ==========================================

        if (cameraFollow != null)
        {
            // 현재 CameraManager가 복귀시킨
            // 정상적인 핀볼 카메라 포즈를 기준으로
            // Follow의 기준값을 다시 저장
            cameraFollow.RefreshDefaultPose();

            // Follow 상태 복구
            cameraFollow.ResumeFollowAfterMonitor(
                cameraGos[0].transform.position,
                cameraGos[0].transform.rotation
            );
        }

        // RoundClear → RoundUI → Monitor 복귀가 끝난 경우
        if (isRoundCameraReturning)
        {
            isRoundCameraReturning = false;

            Debug.Log("[Camera] RoundClear 카메라 복귀 완료 → 게임 재개");

            Time.timeScale = 1f;

            RoundStartEvt?.Invoke();

            return;
        }

        //최초 게임 시작시
        if (isFirstRound)
        {
            isFirstRound = false;

            var roundUI = FindAnyObjectByType<KHS_Script_UIImgFunc>();
            roundUI.StartRoundFunc(scoreManager.RoundRespone() - 1);

            return;
        }

        RoundStartEvt?.Invoke();
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
    private void OnRoundClear()
    {
        Debug.Log("[Round] UI 먼저 실행");
        IsRoundCameraReady = false;

        isRoundFlow = true;

        var roundUI = FindAnyObjectByType<KHS_Script_UIImgFunc>();
        if (roundUI != null)
        {
            roundUI.StartRoundFunc(scoreManager.RoundRespone() - 1);
        }
    }

    private void StartCameraTransition()
    {
        MonitorOn(); // 기존 로직 재사용
    }
    private void OnRoundUIEvent(bool isActive)
    {
        if (!isActive && isRoundFlow)
        {
            Debug.Log("[Camera] Round UI 종료 → 카메라 이동 시작");

            isRoundFlow = false; // 반드시 초기화

            // RoundClear 이후의 UI 종료임을 기록
            isRoundCameraReturning = true;

            //StartCameraTransition();
            IsRoundCameraReady = true;
            return;
        }
        
        Time.timeScale = 1f;
    }
}
