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

    [Header("요구 속도 설정")]
    public float initialRequiredSpeed = 60f;  // 시작 요구 속도
    public float successIncrease = 10f;       // 통과 시 증가량
    public float failDecrease = 10f;          // 실패 시 감소량
    public float minRequiredSpeed = 60f;      // 최저 요구 속도
    public float maxRequiredSpeed = 170f;     // 최고 요구 속도

    private float currentRequiredSpeed;

    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private NoteGenerator noteGenerator;
    [SerializeField] private PatternInput Input;
    [SerializeField] private BackgroundLoop backgroundLoop;
    [SerializeField] private Transform trainTransform;
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private TrainSpeedController trainSpeedController;

    private void Start()
    {
        currentRequiredSpeed = initialRequiredSpeed;
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

    public void OnObstaclePassed()
    {
        trainSpeedController?.OnObstacleResult(true);
        currentRequiredSpeed = Mathf.Min(currentRequiredSpeed + successIncrease, maxRequiredSpeed);
        if (rythmManager != null)
            rythmManager.AddBpmOnNextMeasure(10f);

        rythmManager.RunOnNextMeasure(() => {
            if (Input != null)
                Input.BeginSelection();
        });
    }

    public void OnObstacleFailed()
    {
        trainSpeedController?.OnObstacleResult(false);
        currentRequiredSpeed = Mathf.Max(currentRequiredSpeed - failDecrease, minRequiredSpeed);
        if (healthManager != null)
            healthManager.TakeDamage();

        rythmManager.RunOnNextMeasure(() => {
            if (Input != null)
                Input.BeginSelection();
        });
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
        {
            obstacle.requiredSpeed = currentRequiredSpeed;
            obstacle.Init(this, arrivalDspTime);
        }
    }
}
