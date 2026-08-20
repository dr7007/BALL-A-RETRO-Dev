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

    //[SerializeField]
    //private CJS_Script_CameraFollowBall cameraFollow;
    [SerializeField]
    private KHS_Script_CameraController cameraController;

    // =========================================================
    // Monitor Camera
    // =========================================================

    private Vector3 cameraInitPos;
    private Quaternion cameraInitRot;

    private Vector3 cameraTargetPos;
    private Quaternion cameraTargetRot;


    private Coroutine moveCoroutine;


    // =========================================================
    // Round Flow
    // =========================================================

    private bool isRoundCameraReturning = false;

    private bool isRoundFlow = false;

    private bool isFirstRound = true;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();

        // 반드시 현재 Main Camera의 실제 초기 자세를 저장
        MainCamOn();

        cameraInitPos =
            cameraGos[0].transform.position;

        cameraInitRot =
            cameraGos[0].transform.rotation;

        cameraTargetPos =
            cameraGos[2].transform.position;

        cameraTargetRot =
            cameraGos[2].transform.rotation;


        Debug.Log(
            $"[CameraManager] 초기 Main Pose\n" +
            $"Pos : {cameraInitPos}\n" +
            $"Rot : {cameraInitRot.eulerAngles}"
        );
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


    // =========================================================
    // Main Camera
    // =========================================================

    public void MainCamOn()
    {
        Camera mainCamera = cameraGos[0].GetComponent<Camera>();

        CameraChangeEvt?.Invoke(mainCamera);

        cameraGos[0].SetActive(true);

        cameraGos[1].SetActive(false);

        cameraGos[3].SetActive(false);


        Debug.Log("[CameraManager] Main Camera ON");
    }


    // =========================================================
    // Sub Camera
    // =========================================================

    public void SubCamOn(int index)
    {
        Camera subCamera = cameraGos[index].GetComponent<Camera>();


        CameraChangeEvt?.Invoke(subCamera);

        cameraGos[0].SetActive(false);

        cameraGos[index].SetActive(true);


        Debug.Log($"[CameraManager] Sub Camera ON : {index}");
    }


    // =========================================================
    // Monitor ON
    // =========================================================

    public void MonitorOn(PSH_Script_DialogueUI dialogueUI = null)
    {
        Debug.Log("[CameraManager] MonitorOn");


        // -----------------------------------------------------
        // 1. Controller에게 현재 플레이 카메라 상태 저장 요청
        // -----------------------------------------------------

        if (cameraController != null)
        {
            cameraController.BeginMonitor();
        }


        // -----------------------------------------------------
        // 2. Monitor 이벤트
        // -----------------------------------------------------

        MonitorEvt?.Invoke(true);


        // -----------------------------------------------------
        // 3. Monitor Camera 활성화
        // -----------------------------------------------------

        cameraGos[2].SetActive(true);


        Transform mainCamera = cameraGos[0].transform;


        // 현재 메인 카메라 위치에서 시작
        cameraGos[2].transform.position = mainCamera.position;

        cameraGos[2].transform.rotation = mainCamera.rotation;


        // -----------------------------------------------------
        // 4. 기존 Transition 제거
        // -----------------------------------------------------

        StopMoveCoroutine();


        // -----------------------------------------------------
        // 5. Monitor로 이동
        // -----------------------------------------------------

        moveCoroutine = 
            StartCoroutine(
                MoveCameraSmooth(
                    cameraGos[2].transform,
                    cameraTargetPos,
                    cameraTargetRot,
                    moveDuration,
                    () => {dialogueUI?.Play("Intro");}
                )
            );


        // -----------------------------------------------------
        // 6. Main Camera 비활성화
        // -----------------------------------------------------

        cameraGos[0].SetActive(false);
    }


    // =========================================================
    // Monitor OFF
    // =========================================================

    public void MonitorOff()
    {
        Debug.Log("[CameraManager] MonitorOff");


        // -----------------------------------------------------
        // 1. Main Camera 활성화
        // -----------------------------------------------------

        cameraGos[0].SetActive(true);


        // -----------------------------------------------------
        // 2. 기존 Transition 중단
        // -----------------------------------------------------

        StopMoveCoroutine();


        // -----------------------------------------------------
        // 3. 어디로 돌아갈지 결정
        // -----------------------------------------------------

        Vector3 returnPos;
        Quaternion returnRot;


        if (cameraController != null && cameraController.HasMonitorSavedPose())
        {
            returnPos = cameraController.GetMonitorSavedPosition();

            returnRot = cameraController.GetMonitorSavedRotation();
        }
        else
        {
            // 안전장치
            returnPos = cameraInitPos;

            returnRot = cameraInitRot;
        }


        Debug.Log(
            $"[CameraManager] Monitor Return\n" +
            $"Pos : {returnPos}\n" +
            $"Rot : {returnRot.eulerAngles}"
        );


        // -----------------------------------------------------
        // 4. Monitor Camera에서 Main Camera로 이동
        // -----------------------------------------------------

        cameraGos[0].transform.position = cameraGos[2].transform.position;

        cameraGos[0].transform.rotation = cameraGos[2].transform.rotation;


        // -----------------------------------------------------
        // 5. Main Camera 이동 시작
        // -----------------------------------------------------

        moveCoroutine =
            StartCoroutine(
                MoveCameraSmooth(
                    cameraGos[0].transform,
                    returnPos,
                    returnRot,
                    moveDuration,
                    OnCameraReturnComplete
                )
            );


        // -----------------------------------------------------
        // 6. Monitor Camera 비활성화
        // -----------------------------------------------------

        cameraGos[2].SetActive(false);


        MonitorEvt?.Invoke(false);
    }


    // =========================================================
    // Dialogue
    // =========================================================

    public void MonitorOffAfterDialogue(string str)
    {
        if (str == "Intro")
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


    public void MonitorOnBeforeDialogue(PSH_Script_DialogueUI dialogueUI)
    {
        Cursor.visible = true;

        Cursor.lockState = CursorLockMode.None;

        MonitorOn(dialogueUI);
    }


    // =========================================================
    // Camera Return Complete
    // =========================================================

    private void OnCameraReturnComplete()
    {
        Debug.Log("[CameraManager] Main Camera 복귀 완료");


        // -----------------------------------------------------
        // 가장 중요
        // 여기서 Follow를 다시 활성화
        // -----------------------------------------------------

        if (cameraController != null)
        {
            cameraController.EndMonitor();
        }


        // -----------------------------------------------------
        // RoundClear 이후 복귀
        // -----------------------------------------------------

        if (isRoundCameraReturning)
        {
            isRoundCameraReturning = false;

            IsRoundCameraReady = true;

            Time.timeScale = 1f;


            Debug.Log(
                "[CameraManager] " +
                "RoundClear 카메라 복귀 완료 → 게임 재개"
            );


            RoundStartEvt?.Invoke();

            return;
        }


        // -----------------------------------------------------
        // 최초 게임 시작
        // -----------------------------------------------------

        if (isFirstRound)
        {
            isFirstRound = false;


            var roundUI = FindAnyObjectByType<KHS_Script_UIImgFunc>();


            if (roundUI != null)
            {
                roundUI.StartRoundFunc(scoreManager.RoundRespone() - 1);
            }


            return;
        }


        RoundStartEvt?.Invoke();
    }


    // =========================================================
    // Camera Transition
    // =========================================================

    private IEnumerator MoveCameraSmooth(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration, Action onComplete = null)
    {
        Debug.Log("[CameraTransition] 시작");


        Vector3 startPos = cam.position;

        Quaternion startRot = cam.rotation;


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;


            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);


            cam.position =
                Vector3.Lerp(
                    startPos,
                    targetPos,
                    t
                );


            cam.rotation =
                Quaternion.Slerp(
                    startRot,
                    targetRot,
                    t
                );


            yield return null;
        }


        cam.position = targetPos;

        cam.rotation = targetRot;


        moveCoroutine = null;


        Debug.Log("[CameraTransition] 완료");


        onComplete?.Invoke();
    }


    private void StopMoveCoroutine()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);

            moveCoroutine = null;
        }
    }


    // =========================================================
    // Round Clear
    // =========================================================

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


    // =========================================================
    // Round UI Event
    // =========================================================

    private void OnRoundUIEvent(bool isActive)
    {
        if (!isActive && isRoundFlow)
        {
            Debug.Log("[Camera] Round UI 종료");


            isRoundFlow = false;

            isRoundCameraReturning = true;


            // 여기서는 MonitorOn을 호출하지 않는다.
            //
            // 기존에 이 부분에서
            // StartCameraTransition();
            // 를 호출하면
            //
            // RoundClear
            // → RoundUI
            // → Monitor
            //
            // 흐름이 되어버릴 수 있음.


            IsRoundCameraReady = true;


            return;
        }


        // 최초 Round UI 종료
        Time.timeScale = 1f;
    }
}