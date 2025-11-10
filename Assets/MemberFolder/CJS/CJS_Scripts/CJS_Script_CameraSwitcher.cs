// CJS_Script_CameraSwitcher.cs
using UnityEngine;

public class CJS_Script_CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCam;   // 1층 메인 카메라 (첫 번째 스샷)
    public Camera mazeCam;   // 2층 탑뷰 카메라 (두 번째 스샷 - Orthographic 권장)

    private AudioListener mainListener;
    private AudioListener mazeListener;

    void Awake()
    {
        if (mainCam) mainListener = mainCam.GetComponent<AudioListener>();
        if (mazeCam) mazeListener = mazeCam.GetComponent<AudioListener>();

        // 초기 상태: 메인만 켜기
        SetActive(maze: false, force: true);
    }

    public void ToMaze() { SetActive(maze: true); }
    public void ToMain() { SetActive(maze: false); }

    private void SetActive(bool maze, bool force = false)
    {
        if (!mainCam || !mazeCam) return;

        bool changed = force || mainCam.enabled == maze || mazeCam.enabled != maze;
        if (!changed) return;

        mainCam.enabled = !maze;
        mazeCam.enabled = maze;

        if (mainListener) mainListener.enabled = mainCam.enabled;
        if (mazeListener) mazeListener.enabled = mazeCam.enabled;
    }
}
