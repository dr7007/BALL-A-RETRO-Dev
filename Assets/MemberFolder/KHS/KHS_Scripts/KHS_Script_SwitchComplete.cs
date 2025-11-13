using System;
using UnityEngine;
using UnityEngine.UI;

public class KHS_Script_SwitchComplete : MonoBehaviour
{
    public static event Action kickerOpenevt;

    [SerializeField]
    private ChangeSpriteRenderer[] csRenderers;
    [SerializeField]
    private Toggle[] toggles;

    public bool isComplete = false;
    private int curMount = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isComplete = false;
        curMount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < csRenderers.Length; i++)
        {
            toggles[i].isOn = csRenderers[i].On;
        }
        foreach(var csRenderer in csRenderers)
        {
            if(csRenderer.On)
            {
                curMount++;
            }
        }
        if(curMount >=  csRenderers.Length)
        {
            isComplete = true;
            curMount = 0;
            kickerOpenevt?.Invoke();
            foreach (var csRenderer in csRenderers)
            {
                csRenderer.F_ChangeSprite_Off();
            }
            isComplete = false;
        }
        else
            curMount = 0;
        
    }
}
