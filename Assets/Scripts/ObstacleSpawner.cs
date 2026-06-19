using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("��ֹ� ����")]
    public GameObject[] obstaclePrefabs;
    public Transform spawnPoint;

    [Header("�׽�Ʈ ����")]
    public bool isTestMode = true;
    public KeyCode testSpawnKey = KeyCode.Space;

    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private NoteGenerator noteGenerator;

    private void Start()
    {
        if (noteGenerator != null)
            noteGenerator.OnWaveFinished += OnRhythmSectionComplete;
    }

    private void OnDestroy()
    {
        if (noteGenerator != null)
            noteGenerator.OnWaveFinished -= OnRhythmSectionComplete;
    }

    void Update()
    {
        if (isTestMode && Input.GetKeyDown(testSpawnKey))
            StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    }

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
        if (rythmManager == null) return 2f;
        return rythmManager.SecondsPerMeasure;
    }

    public void OnObstaclePassed()
    {
        if (rythmManager != null)
            rythmManager.ChangeBpmOnNextMeasure(rythmManager.BPM + 10f);
    }

    public void OnObstacleFailed()
    {
        // healthManager.TakeDamage(); ���� ����
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