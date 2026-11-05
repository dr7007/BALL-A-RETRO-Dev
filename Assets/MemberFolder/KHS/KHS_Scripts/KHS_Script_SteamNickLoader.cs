using TMPro;
using UnityEngine;

public class KHS_Script_SteamNickLoader : MonoBehaviour
{
    [SerializeField]
    private SteamManager steamManager = null;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private string str = "";

    private void Awake()
    {
        steamManager = FindAnyObjectByType<SteamManager>();
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (BallARetroPlatform.IsSteamBuild && steamManager != null)
            str = steamManager.GetSteamNickname();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void FillInputFieldToSteamNickname()
    {
        if (!BallARetroPlatform.IsSteamBuild || inputField == null || str == "Unknown" || string.IsNullOrEmpty(str)) return;

        inputField.text = str;
    }
}
