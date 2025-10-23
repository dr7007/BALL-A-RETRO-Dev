using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnReset;
    public string gameSceneName;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            Debug.LogError("Reset!");
            SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }
}
