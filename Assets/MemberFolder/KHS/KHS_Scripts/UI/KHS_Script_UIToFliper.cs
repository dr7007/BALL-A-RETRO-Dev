using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class KHS_Script_UIToFliper : MonoBehaviour
{
    private TextMeshProUGUI fliperUI;
    private KHS_Script_FliperController fliperCon;

    private void Awake()
    {
        fliperCon = FindAnyObjectByType<KHS_Script_FliperController>();
        fliperUI = GetComponent<TextMeshProUGUI>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FliperCountUIUpdate();
    }

    // Update is called once per frame
    void Update()
    {
        KHS_Script_FliperController.FliperCountChangeEvt += FliperCountUIUpdate;
    }

    private void OnEnable()
    {
        KHS_Script_FliperController.FliperCountChangeEvt -= FliperCountUIUpdate;
    }
    
    private void FliperCountUIUpdate()
    {
        fliperUI.text = $"Fliper Count : {fliperCon.fliper_Count}";
    }
}