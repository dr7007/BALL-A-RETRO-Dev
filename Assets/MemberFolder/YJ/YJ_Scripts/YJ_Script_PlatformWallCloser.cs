using UnityEngine;

public class YJ_Script_PlatformWallCloser : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("트리거를 나갔을 때 활성화할 오브젝트")]
    [SerializeField]
    private GameObject targetObjectToEnable;

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetObject;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetObject;
    }

    private void Start()
    {
        ResetObject();
    }

    private void ResetObject()
    {
        if (targetObjectToEnable != null)
        {
            targetObjectToEnable.SetActive(false);
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
            }
        }
    }
}