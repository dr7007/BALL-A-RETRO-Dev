using UnityEngine;

public class CJS_Script_VFXDirector : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstacleHitVfx; // 장애물 충돌 이펙트
    public GameObject deathVfx;       // 게임오버 이펙트

    [Header("Refs")]
    public Transform ballTransform;   // 죽음 이펙트 위치 대체용

    [Header("Options")]
    public float autoDestroySeconds = 2f;

    void OnEnable()
    {
        KHS_Script_DumpManager.OnObstacleHitAt += SpawnHitVfx;
        KHS_Script_ScoreManager.OnGameOver += SpawnDeathVfx;
    }

    void OnDisable()
    {
        KHS_Script_DumpManager.OnObstacleHitAt -= SpawnHitVfx;
        KHS_Script_ScoreManager.OnGameOver -= SpawnDeathVfx;
    }

    private void SpawnHitVfx(Vector3 pos)
    {
        if (obstacleHitVfx == null) return;
        var go = Instantiate(obstacleHitVfx, pos, Quaternion.identity);
        if (autoDestroySeconds > 0f) Destroy(go, autoDestroySeconds);
    }

    private void SpawnDeathVfx()
    {
        if (deathVfx == null) return;
        Vector3 pos = ballTransform != null ? ballTransform.position : Vector3.zero;
        var go = Instantiate(deathVfx, pos, Quaternion.identity);
        if (autoDestroySeconds > 0f) Destroy(go, autoDestroySeconds);
    }
}
