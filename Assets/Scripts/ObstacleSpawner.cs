using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("장애물 설정")]
    public GameObject[] obstaclePrefabs;
    public Transform spawnPoint;

    [Header("테스트 설정")]
    public bool isTestMode = true;
    public KeyCode testSpawnKey = KeyCode.Space;

    [SerializeField] private RythmManager rythmManager;

    void Update()
    {
        if (isTestMode && Input.GetKeyDown(testSpawnKey))
            StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    }

    // 1번 담당이 구간 끝나면 호출
    public void OnRhythmSectionComplete()
    {
        StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnObstacle();
    }

    float GetBarDuration()
    {
        if (rythmManager == null) return 2f; // 연동 안됐을 때 기본값
        return rythmManager.SecondsPerMeasure;
    }

    public void OnObstaclePassed()
    {
        // BPM 증가 - 다음 마디부터 적용
        if (rythmManager != null)
            rythmManager.ChangeBpmOnNextMeasure(rythmManager.BPM + 10f);
    }

    public void OnObstacleFailed()
    {
        // healthManager.TakeDamage(); 추후 연동
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0 || spawnPoint == null) return;

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject obj = Instantiate(obstaclePrefabs[randomIndex], spawnPoint.position, Quaternion.identity);

        Obstacle obstacle = obj.GetComponent<Obstacle>();
        if (obstacle != null)
            obstacle.Init(this);
    }
}