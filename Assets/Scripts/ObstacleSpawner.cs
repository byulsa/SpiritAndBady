using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("장애물")]
    public GameObject[] obstaclePrefabs;

    [Header("요구 속도 설정")]
    public float initialRequiredSpeed = 60f;
    public float successIncrease = 10f;
    public float failDecrease = 10f;
    public float minRequiredSpeed = 60f;
    public float maxRequiredSpeed = 170f;
    public float spawnDistance = 20f;

    [Header("장애물 속도 설정")]
    public float obstacleSpeedScale = 0.1f;
    public float obstacleYOffset = 0.2f;

    public float currentRequiredSpeed;
    private bool isGameOver = false;

    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private NoteGenerator noteGenerator;
    [SerializeField] private PatternInput Input;
    [SerializeField] private Transform trainTransform;
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private TrainSpeedController trainSpeedController;

    [Header("Sound")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip FailSound;
    [SerializeField] private MatControll VignetteMat;

    private void Start()
    {
        currentRequiredSpeed = initialRequiredSpeed;
        if (noteGenerator != null)
            noteGenerator.OnWaveFinished += OnRhythmSectionComplete;
        if (Input != null)
            Input.OnSelectionTimedOut += OnRhythmSectionComplete;
        if (healthManager != null)
            healthManager.OnDead += () => isGameOver = true;
    }

    private void OnDestroy()
    {
        if (noteGenerator != null)
            noteGenerator.OnWaveFinished -= OnRhythmSectionComplete;
        if (Input != null)
            Input.OnSelectionTimedOut -= OnRhythmSectionComplete;
    }

    public void OnRhythmSectionComplete()
    {
        if (rythmManager != null)
        {
            trainSpeedController?.SetRequiredSpeed(currentRequiredSpeed);
            rythmManager.RunOnNextMeasure(SpawnObstacle);
        }
    }

    public void OnObstaclePassed()
    {
        trainSpeedController?.OnObstacleResult(true);
        currentRequiredSpeed = Mathf.Min(currentRequiredSpeed + successIncrease, maxRequiredSpeed);
        if (rythmManager != null)
            rythmManager.AddBpmOnNextMeasure(10f);

        rythmManager.RunOnNextMeasure(() =>
        {
            if (Input != null)
                Input.BeginSelection();
        });

        if (source != null && successSound != null)
            source.PlayOneShot(successSound);
    }

    public void OnObstacleFailed()
    {
        trainSpeedController?.OnObstacleResult(false);
        currentRequiredSpeed = Mathf.Max(currentRequiredSpeed - failDecrease, minRequiredSpeed);

        if (healthManager != null)
            healthManager.TakeDamage();

        if (isGameOver) return;

        if (VignetteMat != null)
            VignetteMat.DamageEffect();

        if (rythmManager != null)
            rythmManager.RunOnNextMeasure(() =>
            {
                if (Input != null)
                    Input.BeginSelection();
            });

        if (source != null && FailSound != null)
            source.PlayOneShot(FailSound);
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0 || trainTransform == null) return;

        bool willCharge = trainSpeedController.GetCurrentSpeed() >= currentRequiredSpeed;
        float timeToHit = willCharge ? rythmManager.SecondsPerBeat * 1f : rythmManager.SecondsPerBeat * 2f;
        float trainAdvancedX = trainTransform.position.x + (willCharge ? trainSpeedController.moveDistance : 0f);

        // 고정 거리에서 생성
        float spawnX = trainAdvancedX + spawnDistance;
        Vector3 spawnPos = new Vector3(spawnX, trainTransform.position.y + obstacleYOffset, trainTransform.position.z);
        Vector3 targetPos = new Vector3(trainAdvancedX, trainTransform.position.y + obstacleYOffset, trainTransform.position.z);

        double arrivalDspTime = AudioSettings.dspTime + timeToHit;

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject obj = Instantiate(obstaclePrefabs[randomIndex], spawnPos, Quaternion.identity);

        Obstacle obstacle = obj.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            obstacle.requiredSpeed = currentRequiredSpeed;
            obstacle.Init(this, arrivalDspTime, targetPos, trainSpeedController);
        }

        trainSpeedController?.TryChargeEffect();
    }
}