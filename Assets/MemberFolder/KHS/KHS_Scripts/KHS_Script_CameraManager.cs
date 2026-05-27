using PSH;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // public void MonitorOn(PSH_Script_DialogueUI _DialogueUI = null)
    // {
    //     MonitorEvt?.Invoke(true);

    //     // cameraGos[2]를 활성화하고 이동 시작점(cameraGos[0] 위치)으로 설정
    //     cameraGos[2].SetActive(true);
    //     cameraGos[2].transform.position = cameraInitPos;
    //     cameraGos[2].transform.rotation = cameraInitRot;

    //     // 기존 코루틴이 돌고 있다면 중단
    //     if (moveCoroutine != null)
    //     {
    //         StopCoroutine(moveCoroutine);
    //         moveCoroutine = null;
    //     }
    //     // 부드럽게 이동 코루틴 시작
    //     moveCoroutine = StartCoroutine(MoveCameraSmooth(cameraGos[2].transform, cameraTargetPos, cameraTargetRot, moveDuration, () => _DialogueUI?.Play("Intro")));

    //     // cameraGos[0]은 비활성화
    //     cameraGos[0].SetActive(false);
    // }

    //psh이 튜토리얼 대사치게할려고 수정함
    public void MonitorOn(PSH_Script_DialogueUI _DialogueUI = null)
    {
        MonitorEvt?.Invoke(true);

        cameraGos[2].SetActive(true);
        cameraGos[2].transform.position = cameraInitPos;
        cameraGos[2].transform.rotation = cameraInitRot;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        
        // 씬 이름에 "Tutorial"이라는 단어가 포함되어 있기만 하면 튜토리얼로 간주!
        // (예: "CJS_Scene_Tutorial", "PSH_TutorialTest" 모두 정상 작동)
        string dialogueKey = currentScene.Contains("Tutorial") ? "Tutorial" : "Intro";

        moveCoroutine = StartCoroutine(MoveCameraSmooth(
            cameraGos[2].transform, 
            cameraTargetPos, 
            cameraTargetRot, 
            moveDuration, 
            () => _DialogueUI?.Play(dialogueKey) 
        ));

        cameraGos[0].SetActive(false);
    }
    public void MonitorOff()
    {
        Debug.LogWarning("[CameraManager] MonitorOff 호출됨!\n호출한 사람:\n" + System.Environment.StackTrace);
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
        if(_str == "Intro" || _str =="Tutorial" )
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

        isRoundFlow = true;

        var roundUI = FindAnyObjectByType<KHS_Script_UIImgFunc>();
        if (roundUI != null)
        {
            roundUI.StartRoundFunc(scoreManager.RoundRespone() - 1);
        }
    }

    private void StartCameraTransition()
    {
        MonitorOff(); // 기존 로직 재사용
    }
    private void OnRoundUIEvent(bool isActive)
    {
        if (!isActive && isRoundFlow)
        {
            Debug.Log("[Camera] Round UI 종료 → 카메라 이동 시작");

            isRoundFlow = false; // 반드시 초기화

            StartCameraTransition();
        }
    }
}
