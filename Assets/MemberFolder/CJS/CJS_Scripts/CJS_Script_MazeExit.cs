// CJS_Script_MazeExit.cs
using UnityEngine;

public class CJS_Script_MazeExit : MonoBehaviour
{
    [Header("Refs")]
    public CJS_Script_CameraSwitcher camSwitch;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        // 2층 미로 클리어 → 메인 카메라 복귀
        if (camSwitch) camSwitch.ToMain();
    }
}
