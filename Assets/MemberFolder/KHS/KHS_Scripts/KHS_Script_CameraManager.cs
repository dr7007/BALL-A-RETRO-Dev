using System;
using UnityEngine;

public class KHS_Script_CameraManager : MonoBehaviour
{
    public static event Action<Camera> CameraChangeEvt;

    public GameObject[] cameraGos;

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
    }
    private void OnDisable()
    {
        KHS_Script_PortalController.portalEvt -= SubCamOn;
        KHS_Script_PlincoFunction.ReturnPortalEvt -= MainCamOn;
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
    public void MonitorOn()
    {
        cameraGos[0].SetActive(false);
        cameraGos[2].SetActive(true);
    }
    public void MonitorOff()
    {
        cameraGos[0].SetActive(true);
        cameraGos[2].SetActive(false);
    }
}
