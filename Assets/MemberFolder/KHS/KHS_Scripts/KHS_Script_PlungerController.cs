using UnityEngine;
using System;
using PSH;
using System.Collections; // [ADD] 이벤트 선언용

public class KHS_Script_PlungerController : MonoBehaviour
{
    // [ADD] 공 발사 이벤트(사운드 연동)
    public static event Action OnBallLaunched;

    [Header("발사 설정")]
    [Tooltip("최소 발사 힘")]
    [SerializeField] private float minForce = 1f;

    [Tooltip("최대 발사 힘")]
    [SerializeField] private float maxForce = 50f;

    [Tooltip("최대 힘까지 도달하는 시간 (초)")]
    [SerializeField] private float chargeTime = 2f;

    // 내부 변수
    private float currentForce;
    [SerializeField]
    private Rigidbody ballRigidbody;
    [SerializeField]
    private bool isBallReady = false;

    [SerializeField]
    private bool isLock = false;

    private void OnEnable()
    {
        KHS_Script_CameraManager.MonitorEvt += PlungerStopFunc;
        KHS_Script_UIImgFunc.RoundUIEvt += PlungerStopFunc;
        PSH_Script_GameSceneDirector.NoIntroStartEvt += NoIntroExceptionFunc;
        PSH_Script_SceneLoader.OnSceneLoadStart += NoIntroExceptionFunc;
        PSH_Script_DialogueUI.DialogueWaitingEvt += PlungerStopFunc;
    }
    private void OnDisable()
    {
        KHS_Script_CameraManager.MonitorEvt -= PlungerStopFunc;
        KHS_Script_UIImgFunc.RoundUIEvt -= PlungerStopFunc;
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= NoIntroExceptionFunc;
        PSH_Script_SceneLoader.OnSceneLoadStart -= NoIntroExceptionFunc;
        PSH_Script_DialogueUI.DialogueWaitingEvt -= PlungerStopFunc;
    }
    private void Awake()
    {
        // 씬 로드 초기에 가장 먼저 구독
        PSH_Script_GameSceneDirector.NoIntroStartEvt += NoIntroExceptionFunc;
        PSH_Script_SceneLoader.OnSceneLoadStart += NoIntroExceptionFunc;
    }


    private void Start()
    {
        isLock = true;
        currentForce = minForce;
    }

    private void Update()
    {
        if (!isBallReady)
        {
            return;
        }
        if (!isLock)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                if (currentForce < maxForce)
                {
                    currentForce += (maxForce - minForce) / chargeTime * Time.deltaTime;
                }
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                Launch();
            }
        }
    }

    private void PlungerStopFunc(bool _start)
    {
        if (!_start)
        {
            Debug.LogError("1초낭비 진입");
            StartCoroutine(SpaceBarLock());
        }
        else
        {
            Debug.LogWarning($"일단 플런저 너 멈춰봐{_start}");
            isLock = _start;
        }
    }

    private IEnumerator SpaceBarLock()
    {
        yield return new WaitForSeconds(2.0f);
        isLock = false;
    }

    private void NoIntroExceptionFunc()
    {
        Debug.LogWarning("아니 멈추지 마");
        isLock = false;
    }
    private void Launch()
    {
        if (ballRigidbody != null)
        {
            ballRigidbody.AddForce(Vector3.forward * currentForce, ForceMode.Impulse);
        }
        currentForce = minForce;

        // [ADD] 발사 직후 브로드캐스트(사운드 용)
        OnBallLaunched?.Invoke();
    }

    // 공이 발사 준비 위치에 들어왔을 때 호출
    private void OnTriggerEnter(Collider other)
    {
        // "Ball" 태그를 가진 오브젝트가 들어왔는지 확인
        if (other.CompareTag("Ball"))
        {
            ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                // 공의 직선 속도와 회전 속도를 즉시 0으로 만들어 튀는 현상을 방지
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;

                isBallReady = true;
            }
        }
    }

    // 공이 발사되어 위치를 벗어났을 때 호출
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            isBallReady = false;
            ballRigidbody = null;
        }
    }
    private void OnDestroy()
    {
        // Awake에서 구독한 건 OnDestroy에서 해제 (OnDisable보다 확실)
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= NoIntroExceptionFunc;
        PSH_Script_SceneLoader.OnSceneLoadStart -= NoIntroExceptionFunc;
    }
}
