using UnityEngine;

public class KHS_Script_FirstContactJudge : MonoBehaviour
{
    [Header("카메라 설정")]
    [Tooltip("2층 레이어 관리를 위한 카메라")]
    public Camera mainCam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        YJ_Script_BallController ball = other.GetComponent<YJ_Script_BallController>();
        if (ball != null)
        {
            mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("2F"));
        }
    }
}
