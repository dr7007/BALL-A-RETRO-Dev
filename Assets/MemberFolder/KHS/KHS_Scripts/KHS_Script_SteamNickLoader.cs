using TMPro;
using Unity.VisualScripting;
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
        inputField = GetComponent<TMP_InputField>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        str = steamManager.GetSteamNickname();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void FillInputFieldToSteamNickname()
    {
        if (str == "Unknown" || string.IsNullOrEmpty(str)) return;

        inputField.text = str;
    }
}
