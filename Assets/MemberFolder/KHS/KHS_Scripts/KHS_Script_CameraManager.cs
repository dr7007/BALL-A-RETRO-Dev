using UnityEngine;

public class KHS_Script_CameraManager : MonoBehaviour
{
    [SerializeField]
    private GameObject[] cameraGos;

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

    public void MainCamOn()
    {
        isMain = true;
        cameraGos[0].SetActive(true);
        cameraGos[1].SetActive(false);
    }
    public void SubCamOn()
    {
        isMain = false;
        cameraGos[0].SetActive(false);
        cameraGos[1].SetActive(true);
    }
}
