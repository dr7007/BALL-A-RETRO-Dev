using UnityEngine;

// 팩맨 스테이지의 '코인' 오브젝트에 부착할 스크립트
public class YJ_Script_PacManCoin : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이 코인을 먹었을 때 추가할 점수")]
    public int coinScore = 100;

    [Tooltip("먹었을 때 재생할 사운드 (선택 사항)")]
    public AudioClip collectSound;

    private KHS_Script_ScoreManager scoreManager;
    private bool isCollected = false;

    void Start()
    {
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && gameObject.activeSelf)
        {
            isCollected = true;

            if (scoreManager != null)
            {
                scoreManager.AddScore(coinScore);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            gameObject.SetActive(false);
        }
    }

    public void ActivateCoin()
    {
        isCollected = false;
        gameObject.SetActive(true);
    }

    public void DesactivateCoin()
    {
        gameObject.SetActive(false);
    }
}