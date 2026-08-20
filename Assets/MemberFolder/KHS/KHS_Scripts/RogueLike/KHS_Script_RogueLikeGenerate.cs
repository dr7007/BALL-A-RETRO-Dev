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
        Debug.LogError(
        $"[ROGUELIKE 1] RogueLikeGenerateTime 호출 / " +
        $"TimeScale = {Time.timeScale}");

        StartCoroutine(WaitDeathEffect());
    }

    private void GameOverUnactiveUI()
    {
        transform.parent.gameObject.SetActive(false);
    }

    private IEnumerator WaitDeathEffect()
    {
        Debug.LogError(
        $"[ROGUELIKE 2] Coroutine 시작 / " +
        $"TimeScale = {Time.timeScale}"
        );

        yield return null;

        Debug.LogError(
        $"[ROGUELIKE 3] yield return null 이후 / " +
        $"TimeScale = {Time.timeScale}"
        );

        button.onClick.Invoke();
    }

}
