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

    // TODO: BPM 담당 스크립트 연동 후 아래 주석 해제
    // private RhythmManager rhythmManager;

    void Update()
    {
        if (isTestMode && Input.GetKeyDown(testSpawnKey))
            StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    }

    // 리듬 노드 담당이 구간 끝나면 호출
    public void OnRhythmSectionComplete()
    {
        StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnObstacle();
    }

    // TODO: BPM 담당한테서 받아오는 걸로 교체 예정
    float GetBarDuration()
    {
        return 2f; // BPM 담당 연동 전까지 임시값
    }

    public void OnObstaclePassed()
    {
        // TODO: BPM 담당한테 스테이지 증가 알리기
        // rhythmManager.OnStageCleared();
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