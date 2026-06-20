using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("장애물")]
    public GameObject[] obstaclePrefabs;
    public Transform spawnPoint;

    [Header("테스트용 소환")]
    public bool isTestMode = true;
    public KeyCode testSpawnKey = KeyCode.Space;

    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private NoteGenerator noteGenerator;
    [SerializeField] private PatternInput Input;
    [SerializeField] private BackgroundLoop backgroundLoop;
    [SerializeField] private Transform trainTransform;

    private void Start()
    {
        if (noteGenerator != null)
            noteGenerator.OnWaveFinished += OnRhythmSectionComplete;
        if (Input != null)
            Input.OnSelectionTimedOut += OnRhythmSectionComplete;
    }

    private void OnDestroy()
    {
        if (noteGenerator != null)
            noteGenerator.OnWaveFinished -= OnRhythmSectionComplete;
        if (Input != null)
            Input.OnSelectionTimedOut -= OnRhythmSectionComplete;
    }

    void Update()
    {
        // if (isTestMode && Input.GetKeyDown(testSpawnKey))
        //     StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    }

    public void OnRhythmSectionComplete()
    {
        if (rythmManager != null)
        {
            if (rythmManager != null)
                rythmManager.RunOnNextMeasure(SpawnObstacle);
        }
    }
    // public void OnRhythmSectionComplete()
    // {
    //     StartCoroutine(SpawnAfterDelay(GetBarDuration()));
    // }

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
        if (obstaclePrefabs.Length == 0 || trainTransform == null) return;

        float timeToHit = rythmManager.SecondsPerBeat * 2f;
        float spawnX = trainTransform.position.x + (backgroundLoop.currentSpeed * timeToHit);
        Vector3 spawnPos = new Vector3(spawnX, trainTransform.position.y, trainTransform.position.z);

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject obj = Instantiate(obstaclePrefabs[randomIndex], spawnPos, Quaternion.identity);

        double arrivalDspTime = AudioSettings.dspTime + timeToHit;

        Debug.Log($"장애물 스폰 / timeToHit: {timeToHit:F3}초 / spawnX: {spawnX:F2}");

        Obstacle obstacle = obj.GetComponent<Obstacle>();
        if (obstacle != null)
            obstacle.Init(this, arrivalDspTime);
    }
}