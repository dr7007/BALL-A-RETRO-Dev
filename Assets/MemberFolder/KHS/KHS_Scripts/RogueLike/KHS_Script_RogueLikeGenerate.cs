using UnityEngine;
using UnityEngine.UI;

public class KHS_Script_RogueLikeGenerate : MonoBehaviour
{
    [SerializeField]
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        KHS_Script_ScoreManager.Next_Round_Init += RogueLikeGenerateTime;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.Next_Round_Init -= RogueLikeGenerateTime;
    }

    private void RogueLikeGenerateTime()
    {
        button.onClick.Invoke();
    }
}
