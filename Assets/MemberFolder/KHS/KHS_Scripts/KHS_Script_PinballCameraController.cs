using System.Collections;
using UnityEngine;

public class KHS_Script_PinballCameraController : MonoBehaviour
{
    public enum CameraState
    {
        Normal,
        FollowBall,
        Monitor
    }

    [Header("References")]
    [SerializeField] private Transform ball;

    [Tooltip("핀볼판 전체의 기준 Transform")]
    [SerializeField] private Transform boardRoot;

    [Tooltip("게임 시작 시 핀볼판을 바라보는 기본 카메라 위치")]
    [SerializeField] private Transform normalCameraPoint;

    [Tooltip("특정 점수 달성 시 이동할 모니터 카메라 위치")]
    [SerializeField] private Transform monitorCameraPoint;


    [Header("Follow Settings")]
    [Range(0f, 1f)]
    [Tooltip("핀볼판 아래에서 위까지 중 몇 % 지점부터 추적할지")]
    [SerializeField] private float followStartPercent = 0.4f;

    [Range(0f, 1f)]
    [Tooltip("Follow 상태에서 공이 화면상 위치할 비율")]
    [SerializeField] private float followScreenPercent = 0.4f;

    [SerializeField] private float followSmoothTime = 0.15f;

    [Tooltip("핀볼판의 좌우 움직임은 따라가지 않음")]
    [SerializeField] private bool followBoardHorizontal = false;


    [Header("Monitor Movement")]
    [SerializeField] private float moveToMonitorTime = 0.7f;

    [SerializeField] private float returnFromMonitorTime = 0.7f;


    private CameraState currentState = CameraState.Normal;

    private Vector3 normalPosition;
    private Quaternion normalRotation;

    private Vector3 followVelocity;

    private Coroutine cameraRoutine;


    private void Awake()
    {
        if (ball == null)
        {
            GameObject ballObject =
                GameObject.FindWithTag("Ball");

            if (ballObject != null)
                ball = ballObject.transform;
        }

        SaveNormalCamera();
    }


    private void LateUpdate()
    {
        if (ball == null || boardRoot == null)
            return;


        switch (currentState)
        {
            case CameraState.Normal:

                CheckFollowStart();

                break;


            case CameraState.FollowBall:

                FollowBall();

                break;


            case CameraState.Monitor:

                // 모니터 연출 중에는 아무것도 하지 않는다.
                break;
        }
    }


    // =========================================================
    // NORMAL
    // =========================================================

    private void CheckFollowStart()
    {
        float ballPercent =
            GetBallBoardHeightPercent();


        if (ballPercent >= followStartPercent)
        {
            StartBallFollow();
        }
    }


    // =========================================================
    // 핀볼판 기준 공의 높이
    // =========================================================

    private float GetBallBoardHeightPercent()
    {
        Vector3 localBallPosition =
            boardRoot.InverseTransformPoint(ball.position);


        /*
         * boardRoot의 로컬 Y를 기준으로
         *
         * 아래 = 0
         * 위   = 1
         *
         * 로 정규화한다.
         *
         * 따라서 핀볼판이 월드 기준으로
         * 기울어져 있어도 상관없다.
         */


        float bottomY = 0f;

        float topY = GetBoardTopLocalY();


        if (Mathf.Abs(topY - bottomY) < 0.001f)
            return 0f;


        float percent =
            (localBallPosition.y - bottomY)
            / (topY - bottomY);


        return Mathf.Clamp01(percent);
    }


    private float GetBoardTopLocalY()
    {
        /*
         * normalCameraPoint와 별개로
         * boardRoot의 스케일/회전에 영향을 받지 않도록
         * Inspector에서 직접 지정할 수도 있지만,
         * 여기서는 BoardTopMarker를 사용하는 방식을 추천한다.
         */

        if (boardTopMarker != null)
        {
            return boardRoot.InverseTransformPoint(
                boardTopMarker.position
            ).y;
        }

        return 1f;
    }


    [Header("Board Markers")]
    [SerializeField] private Transform boardTopMarker;


    // =========================================================
    // FOLLOW START
    // =========================================================

    private void StartBallFollow()
    {
        currentState =
            CameraState.FollowBall;

        followVelocity =
            Vector3.zero;
    }


    // =========================================================
    // FOLLOW
    // =========================================================

    private void FollowBall()
    {
        Vector3 localBall =
            boardRoot.InverseTransformPoint(
                ball.position
            );


        /*
         * 현재 공의 핀볼판 로컬 Y 위치를 가져온다.
         *
         * 카메라는 핀볼판의 로컬 Y축 방향으로만 이동한다.
         */


        Vector3 localCamera =
            boardRoot.InverseTransformPoint(
                transform.position
            );


        /*
         * 공과 카메라의 상대적인 핀볼판 로컬 Y 차이.
         *
         * 처음 Follow에 들어왔을 때의 화면 구성을
         * 유지하기 위해 현재 차이를 사용한다.
         */

        float desiredLocalY =
            localBall.y
            + GetFollowOffsetLocalY();


        localCamera.y =
            desiredLocalY;


        Vector3 targetWorld =
            boardRoot.TransformPoint(
                localCamera
            );


        /*
         * 좌우 움직임은 기본적으로 막는다.
         *
         * 핀볼판이 기울어져 있더라도
         * boardRoot 기준으로 움직인다.
         */

        if (!followBoardHorizontal)
        {
            Vector3 originalLocal =
                boardRoot.InverseTransformPoint(
                    transform.position
                );

            Vector3 targetLocal =
                boardRoot.InverseTransformPoint(
                    targetWorld
                );

            targetLocal.x =
                originalLocal.x;

            targetWorld =
                boardRoot.TransformPoint(
                    targetLocal
                );
        }


        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetWorld,
                ref followVelocity,
                followSmoothTime
            );
    }


    private float followOffsetLocalY;


    private float GetFollowOffsetLocalY()
    {
        /*
         * 최초 호출 시 현재 카메라와 공 사이의
         * 핀볼판 로컬 Y 차이를 저장한다.
         */

        if (!followOffsetInitialized)
        {
            Vector3 localBall =
                boardRoot.InverseTransformPoint(
                    ball.position
                );

            Vector3 localCamera =
                boardRoot.InverseTransformPoint(
                    transform.position
                );

            followOffsetLocalY =
                localCamera.y - localBall.y;

            followOffsetInitialized = true;
        }


        return followOffsetLocalY;
    }


    private bool followOffsetInitialized = false;


    // =========================================================
    // MONITOR CAMERA
    // =========================================================

    public void MoveToMonitor()
    {
        if (monitorCameraPoint == null)
        {
            Debug.LogWarning(
                "Monitor Camera Point가 지정되지 않았습니다."
            );

            return;
        }


        if (cameraRoutine != null)
            StopCoroutine(cameraRoutine);


        cameraRoutine =
            StartCoroutine(
                CoMoveToMonitor()
            );
    }


    private IEnumerator CoMoveToMonitor()
    {
        currentState =
            CameraState.Monitor;


        followVelocity =
            Vector3.zero;


        Vector3 startPosition =
            transform.position;

        Quaternion startRotation =
            transform.rotation;


        float elapsed = 0f;


        while (elapsed < moveToMonitorTime)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / moveToMonitorTime
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.position =
                Vector3.Lerp(
                    startPosition,
                    monitorCameraPoint.position,
                    t
                );


            transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    monitorCameraPoint.rotation,
                    t
                );


            yield return null;
        }


        transform.position =
            monitorCameraPoint.position;

        transform.rotation =
            monitorCameraPoint.rotation;


        cameraRoutine = null;
    }


    // =========================================================
    // RETURN FROM MONITOR
    // =========================================================

    public void ReturnFromMonitor()
    {
        if (cameraRoutine != null)
            StopCoroutine(cameraRoutine);


        cameraRoutine =
            StartCoroutine(
                CoReturnFromMonitor()
            );
    }


    private IEnumerator CoReturnFromMonitor()
    {
        currentState =
            CameraState.Monitor;


        Vector3 startPosition =
            transform.position;

        Quaternion startRotation =
            transform.rotation;


        float elapsed = 0f;


        while (elapsed < returnFromMonitorTime)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / returnFromMonitorTime
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.position =
                Vector3.Lerp(
                    startPosition,
                    normalPosition,
                    t
                );


            transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    normalRotation,
                    t
                );


            yield return null;
        }


        // 정확하게 기본 카메라 위치 복원
        transform.position =
            normalPosition;

        transform.rotation =
            normalRotation;


        followVelocity =
            Vector3.zero;


        /*
         * 매우 중요.
         *
         * 이전 Follow 상태의 offset을 버린다.
         *
         * 모니터 연출 전의 공 위치를 기준으로
         * 이상한 Follow가 이어지는 것을 방지한다.
         */

        followOffsetInitialized =
            false;


        currentState =
            CameraState.Normal;


        cameraRoutine = null;
    }


    // =========================================================
    // NORMAL CAMERA 저장
    // =========================================================

    private void SaveNormalCamera()
    {
        if (normalCameraPoint != null)
        {
            normalPosition =
                normalCameraPoint.position;

            normalRotation =
                normalCameraPoint.rotation;
        }
        else
        {
            normalPosition =
                transform.position;

            normalRotation =
                transform.rotation;
        }
    }


    // =========================================================
    // 외부에서 기본 카메라 위치 갱신
    // =========================================================

    public void RefreshNormalCamera()
    {
        SaveNormalCamera();

        followOffsetInitialized =
            false;

        currentState =
            CameraState.Normal;
    }


    // =========================================================
    // 현재 상태 확인
    // =========================================================

    public CameraState GetCameraState()
    {
        return currentState;
    }
}