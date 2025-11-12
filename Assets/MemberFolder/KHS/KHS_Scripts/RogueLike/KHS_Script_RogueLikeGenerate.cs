using System.Collections;
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
        KHS_Script_ScoreManager.OnGameClear += GameOverUnactiveUI;
        KHS_Script_ScoreManager.OnGameOver += GameOverUnactiveUI;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.Next_Round_Init -= RogueLikeGenerateTime;
        KHS_Script_ScoreManager.OnGameClear -= GameOverUnactiveUI;
        KHS_Script_ScoreManager.OnGameOver -= GameOverUnactiveUI;
    }

    private void RogueLikeGenerateTime()
    {
        StartCoroutine(WaitDeathEffect());
    }

    private void GameOverUnactiveUI()
    {
        transform.parent.gameObject.SetActive(false);
    }

    private IEnumerator WaitDeathEffect()
    {
        yield return null;
        button.onClick.Invoke();
    }

}
