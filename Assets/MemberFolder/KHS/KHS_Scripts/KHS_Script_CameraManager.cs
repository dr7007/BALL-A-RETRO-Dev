using PSH;
using System;
using System.Collections;
using UnityEngine;

public class KHS_Script_CameraManager : MonoBehaviour
{
    public static event Action<Camera> CameraChangeEvt;

    public GameObject[] cameraGos;

    [SerializeField] 
    private float moveDuration = 2f; // 이동 시간

    private Vector3 cameraInitPos = Vector3.zero;
    private Quaternion cameraInitRot = Quaternion.identity;
    private Vector3 cameraTargetPos = Vector3.zero;
    private Quaternion cameraTargetRot = Quaternion.identity;
    private Coroutine moveCoroutine;
    private bool isMain = true;

    private void Start()
    {
        isMain = true;
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            if(isMain)
                SubCamOn();
            else
                MainCamOn();
        }
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
        isMain = true;
        CameraChangeEvt.Invoke(cameraGos[0].GetComponent<Camera>());
        cameraGos[0].SetActive(true);
        cameraGos[1].SetActive(false);
    }
    public void SubCamOn()
    {
        isMain = false;
        CameraChangeEvt.Invoke(cameraGos[1].GetComponent<Camera>());
        cameraGos[0].SetActive(false);
        cameraGos[1].SetActive(true);
    }
    public void MonitorOn(PSH_Script_DialogueUI _DialogueUI = null)
    {
        // 목표 위치 (cameraGos[2] 원래 위치) 저장
        cameraTargetPos = cameraGos[2].transform.position;
        cameraTargetRot = cameraGos[2].transform.rotation;

        // cameraGos[2]를 활성화하고 이동 시작점(cameraGos[0] 위치)으로 설정
        cameraGos[2].SetActive(true);
        cameraGos[2].transform.position = cameraGos[0].transform.position;
        cameraGos[2].transform.rotation = cameraGos[0].transform.rotation;

        // 기존 코루틴이 돌고 있다면 중단
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        // 부드럽게 이동 코루틴 시작
        moveCoroutine = StartCoroutine(MoveCameraSmooth(cameraGos[2].transform, cameraTargetPos, cameraTargetRot, moveDuration, _DialogueUI));

        // cameraGos[0]은 비활성화
        cameraGos[0].SetActive(false);
    }
    public void MonitorOff()
    {
        // 목표 위치 (cameraGos[0] 원래 위치) 저장
        cameraTargetPos = cameraGos[0].transform.position;
        cameraTargetRot = cameraGos[0].transform.rotation;

        // cameraGos[0]를 활성화하고 이동 시작점(cameraGos[2] 위치)으로 설정
        cameraGos[0].SetActive(true);
        cameraGos[0].transform.position = cameraGos[2].transform.position;
        cameraGos[0].transform.rotation = cameraGos[2].transform.rotation;

        // 기존 코루틴이 돌고 있다면 중단
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        // 부드럽게 이동 코루틴 시작
        moveCoroutine = StartCoroutine(MoveCameraSmooth(cameraGos[0].transform, cameraTargetPos, cameraTargetRot, moveDuration));

        // cameraGos[0]은 비활성화
        cameraGos[2].SetActive(false);
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
        MonitorOn(null);
    }
    
    public void MonitorOnBeforeDialogue(PSH_Script_DialogueUI _dialogueUI)
    {
        MonitorOn(_dialogueUI);
    }
    private IEnumerator MoveCameraSmooth(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration, PSH_Script_DialogueUI _DialogueUI = null)
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

        if (_DialogueUI != null)
        {
            _DialogueUI.Play("Intro");
        }
    }
}
