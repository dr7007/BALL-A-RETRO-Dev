using UnityEngine;

public class YJ_Script_PlatformWallCloser : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("트리거를 나갔을 때 활성화할 오브젝트")]
    [SerializeField]
    private GameObject targetObjectToEnable;

    private Collider triggerCollider;

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetObject;

        YJ_Script_PacManExit.OnPacManExit += ResetObject;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetObject;

        YJ_Script_PacManExit.OnPacManExit -= ResetObject;
    }

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogError("YJ_Script_PlatformWallCloser에 'Is Trigger'가 켜진 Collider가 없습니다!");
        }

        ResetObject();
    }

    private void ResetObject()
    {
        if (targetObjectToEnable != null)
        {
            targetObjectToEnable.SetActive(false);
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (targetObjectToEnable != null)
            {
                targetObjectToEnable.SetActive(true);
                Debug.Log(targetObjectToEnable.name + "이(가) 활성화되었습니다.");
            }

            YJ_Script_BallController ballController = other.GetComponent<YJ_Script_BallController>();

            if (ballController != null)
            {
                float platformYLevel = other.transform.position.y;
                ballController.Enter2DMode(platformYLevel);

                ballController.SetControlMode(YJ_Script_BallController.ControlMode.PacMan);

                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }
        }
    }
}