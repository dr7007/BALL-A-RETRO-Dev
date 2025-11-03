using System.Collections;
using UnityEngine;
using System; // [수정] Action (이벤트)를 사용하기 위해 추가

public class YJ_Script_WormholeController : MonoBehaviour
{
    [Header("출구 웜홀 지정")]
    [Tooltip("공이 튀어나갈 출구 웜홀")]
    [SerializeField] private YJ_Script_WormholeController exitWormhole;

    [Header("웜홀 대기 시간")]
    [Tooltip("공이 웜홀 안에 머무는 시간 (초)")]
    [SerializeField] private float waitTime = 2f;

    [Header("웜홀 사용 횟수")]
    [Tooltip("웜홀을 사용할 수 있는 횟수, -1로 설정하면 무제한")]
    [SerializeField] private int activationCount = -1;

    [Header("출구로 나온 공에 적용할 가속도")]
    [Tooltip("가본값: 1(들어간 속도 그대로 사출)")]
    [SerializeField] private float acceleration = 1f;

    // --- [수정] 상태 변수를 프로퍼티로 변경 ---

    /// <summary>
    /// 웜홀의 활성화 상태가 변경될 때 호출되는 이벤트입니다.
    /// (bool: 새로운 활성화 상태)
    /// </summary>
    public event Action<bool> OnActivationChanged;

    // 웜홀의 실제 활성화 상태를 저장하는 내부 변수 (Backing Field)
    private bool _isActivated = true;

    /// <summary>
    /// 웜홀이 공을 받아들일 수 있는 상태인지 확인하고 설정합니다.
    /// 상태가 변경될 때 OnActivationChanged 이벤트가 호출됩니다.
    /// </summary>
    public bool isActivated
    {
        get { return _isActivated; }
        private set
        {
            if (_isActivated != value)
            {
                // ▼▼▼ 이 Debug.Log 라인을 추가하세요 ▼▼▼
                Debug.Log($"[YJ_Script] {this.gameObject.name}의 isActivated 상태 변경: {value}", this.gameObject);

                _isActivated = value;
                OnActivationChanged?.Invoke(_isActivated);
            }
        }
    }
    // --- [수정] 끝 ---


    private Transform spawnPoint;


    private void Start()
    {
        spawnPoint = GetComponent<Transform>();
    }

    private void OnDrawGizmos()
    {
        if (exitWormhole != null)
        {
            Gizmos.color = (activationCount == 0) ? Color.gray : Color.cyan;
            Gizmos.DrawLine(transform.position, exitWormhole.transform.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isUsable = (activationCount > 0 || activationCount == -1);

        // [수정] public bool isActivated 변수 대신 프로퍼티(isActivated)를 사용
        if (other.CompareTag("Ball") && isActivated && isUsable)
        {
            if (exitWormhole == null)
            {
                Debug.LogError("출구 웜홀이 지정되지 않았습니다!", this.gameObject);
                return;
            }

            Rigidbody ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                if (activationCount > 0)
                {
                    activationCount--;
                }
            }

            StartCoroutine(TeleportCoroutine(ballRigidbody));
        }
    }

    private IEnumerator TeleportCoroutine(Rigidbody rb)
    {
        // [수정] 프로퍼티의 set 접근자를 호출 (이벤트가 발생함)
        this.isActivated = false;
        exitWormhole.isActivated = false;

        Vector3 incomingVelocity = rb.linearVelocity;
        rb.isKinematic = true;
        rb.gameObject.SetActive(false);

        yield return new WaitForSeconds(waitTime);

        rb.transform.position = exitWormhole.spawnPoint.position;
        rb.gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.linearVelocity = incomingVelocity * acceleration;

        yield return new WaitForSeconds(0.5f);

        // [수정] 프로퍼티의 set 접근자를 호출 (이벤트가 발생함)
        exitWormhole.isActivated = true;

        // [수정] 프로퍼티의 set 접근자를 호출 (이벤트가 발생함)
        this.isActivated = true;
    }

    /// <summary>
    /// 웜홀의 활성화 상태를 외부에서 변경합니다.
    /// </summary>
    /// <param name="value">설정할 활성화 상태</param>
    public void SetActivated(bool value)
    {
        // [수정] 프로퍼티의 set 접근자를 호출 (이벤트가 발생함)
        this.isActivated = value;
    }
}