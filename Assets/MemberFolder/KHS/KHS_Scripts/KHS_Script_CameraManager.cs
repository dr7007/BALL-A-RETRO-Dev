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

    // 추적 Controller의 Transform은 CameraHolder일 수 있다.
    // 실제 플레이 카메라의 월드 자세는 Manager가 별도로 보관한다.
    private Vector3 monitorReturnPos;
    private Quaternion monitorReturnRot;
    private bool monitorActive;
    private bool monitorReturning;
    private bool gameEnded;


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
        KHS_Script_ScoreManager.OnGameOver += OnGameEnded;
        KHS_Script_ScoreManager.OnGameClear += OnGameEnded;
        KHS_Script_UIImgFunc.RoundUIEvt += OnRoundUIEvent;
        KHS_Script_PortalController.portalEvt += SubCamOn;
        KHS_Script_PlincoFunction.ReturnPortalEvt += MainCamOn;


        PSH_Script_GameSceneDirector.OpeningEyeEvt += MonitorOnBeforeDialogue;
        PSH_Script_DialogueUI.DialogueEvt += MonitorOffAfterDialogue;
    }


    private void OnDisable()
    {
        KHS_Script_ScoreManager.Round_Clear -= OnRoundClear;
        KHS_Script_ScoreManager.OnGameOver -= OnGameEnded;
        KHS_Script_ScoreManager.OnGameClear -= OnGameEnded;
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
        if (monitorActive) return;
        if (ActivateOnly(0)) CameraChangeEvt?.Invoke(cameraGos[0].GetComponent<Camera>());
    }

    public void SubCamOn(int index)
    {
        // A late portal callback cannot steal a round/choice presentation.
        if (monitorActive || isRoundFlow || isRoundCameraReturning || gameEnded || index == 2) return;
        if (ActivateOnly(index)) CameraChangeEvt?.Invoke(cameraGos[index].GetComponent<Camera>());
    }

    bool ActivateOnly(int index)
    {
        if (cameraGos == null || index < 0 || index >= cameraGos.Length || !cameraGos[index]) return false;
        for (int i = 0; i < cameraGos.Length; i++)
            if (cameraGos[i]) cameraGos[i].SetActive(i == index);
        return true;
    }

    void OnGameEnded() => gameEnded = true;

    // =========================================================
    // Monitor ON
    // =========================================================

    public void MonitorOn(PSH_Script_DialogueUI dialogueUI = null)
    {
        if (monitorActive || gameEnded)
            return;

        monitorActive = true;
        Debug.Log("[CameraManager] MonitorOn");

        Transform mainCamera = cameraGos[0].transform;
        mainCamera.GetComponent<CJS_Script_CameraShaker>()?.CancelShake();
        monitorReturnPos = mainCamera.position;
        monitorReturnRot = mainCamera.rotation;

        // 기존 씬의 cameraFollow 연결은 새 Controller 필드로 자동 이관되지 않는다.
        if (cameraController == null)
            cameraController = mainCamera.GetComponentInParent<KHS_Script_CameraController>(true);


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

        ActivateOnly(2);


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
        if (!monitorActive || monitorReturning)
            return;

        monitorReturning = true;
        Debug.Log("[CameraManager] MonitorOff");


        // -----------------------------------------------------
        // 1. 복귀가 끝날 때까지 Monitor Camera를 사용한다.
        // -----------------------------------------------------

        // -----------------------------------------------------
        // 2. 기존 Transition 중단
        // -----------------------------------------------------

        StopMoveCoroutine();


        // -----------------------------------------------------
        // 3. 어디로 돌아갈지 결정
        // -----------------------------------------------------

        Vector3 returnPos;
        Quaternion returnRot;


        returnPos = monitorReturnPos;
        returnRot = monitorReturnRot;


        Debug.Log(
            $"[CameraManager] Monitor Return\n" +
            $"Pos : {returnPos}\n" +
            $"Rot : {returnRot.eulerAngles}"
        );


        // -----------------------------------------------------
        // 4. Monitor Camera만 저장된 플레이 시점으로 이동한다.
        // Main Camera의 로컬 위치를 바꾸면 추적 중인 부모와의 간격이 틀어진다.
        // -----------------------------------------------------

        // -----------------------------------------------------
        // 5. Monitor Camera 복귀 시작
        // -----------------------------------------------------

        moveCoroutine =
            StartCoroutine(
                MoveCameraSmooth(
                    cameraGos[2].transform,
                    returnPos,
                    returnRot,
                    moveDuration,
                    OnCameraReturnComplete
                )
            );


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
        if (gameEnded) return;
        var demo = BallARetroDemoRun.Current;
        if (demo != null && demo.IsEnabled && !demo.CanChoose(scoreManager != null ? scoreManager.RoundRespone() : 1)) return;
        Cursor.visible = true;

        Cursor.lockState = CursorLockMode.None;

        MonitorOn(null);
    }


    public void MonitorOnBeforeDialogue(PSH_Script_DialogueUI dialogueUI)
    {
        if (gameEnded) return;
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

        ActivateOnly(0);
        monitorActive = false;
        monitorReturning = false;


        // -----------------------------------------------------
        // 가장 중요
        // 여기서 Follow를 다시 활성화
        // -----------------------------------------------------

        if (cameraController != null)
        {
            cameraController.EndMonitor();
        }

        CameraChangeEvt?.Invoke(cameraGos[0].GetComponent<Camera>());
        MonitorEvt?.Invoke(false);
        if (gameEnded) return;


        // -----------------------------------------------------
        // RoundClear 이후 복귀
        // -----------------------------------------------------

        if (isRoundCameraReturning)
        {
            isRoundCameraReturning = false;

            IsRoundCameraReady = true;

            PSH_Script_GameplayInput.Resume();


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
        // RoundUI must be visible even when the winning score came from roulette.
        MainCamOn();

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
        if (!isActive && !gameEnded) PSH_Script_GameplayInput.Resume();
    }
}
