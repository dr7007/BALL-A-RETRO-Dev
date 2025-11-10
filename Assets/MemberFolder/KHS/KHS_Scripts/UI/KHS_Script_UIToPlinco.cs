using TMPro;
using UnityEngine;

public class KHS_Script_UIToPlinco : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI[] PlincoScoreUIs;
    [SerializeField]
    private KHS_Script_PlincoFunction[] PlincoFuncs;

    public GameObject PlincoColliderHolder;

    void Awake()
    {
        PlincoFuncs = PlincoColliderHolder.GetComponentsInChildren<KHS_Script_PlincoFunction>();
        PlincoScoreUIs = GetComponentsInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        UpdatePlincoUI();
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatePlincoUI()
    {
        for(int i = 0; i < PlincoFuncs.Length; i++)
        {
            PlincoScoreUIs[i].text = $"X{PlincoFuncs[i].ScoreMulti_Response()}";
        }
    }
}
