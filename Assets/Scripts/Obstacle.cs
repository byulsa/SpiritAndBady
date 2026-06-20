using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("장애물 설정")]
    public float requiredSpeed = 50f;

    [Header("파괴 설정")]
    [SerializeField] private Rigidbody[] Rocks;
    [SerializeField, Min(0f)] private float crashForceMultiplier = 1f;
    [SerializeField, Range(0f, 2f)] private float directionNoise = 0.75f;

    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;
    private double arrivalDspTime;
    private double spawnDspTime;
    private Vector3 spawnPosition;
    private Vector3 targetPosition;
    private bool isInitialized = false;
    private bool hasJudged = false;
    private bool hasCrashed = false;

    void Start()
    {
        speedController = FindAnyObjectByType<TrainSpeedController>();

        if (Rocks == null || Rocks.Length == 0)
            Rocks = GetComponentsInChildren<Rigidbody>(true);
    }

    public void Init(ObstacleSpawner obstacleSpawner, double arrivalTime, Vector3 target, TrainSpeedController sc)
    {
        spawner = obstacleSpawner;
        arrivalDspTime = arrivalTime;
        spawnDspTime = AudioSettings.dspTime;
        spawnPosition = transform.position;
        targetPosition = target;
        speedController = sc;
        isInitialized = true;
        Debug.Log($"장애물 생성 / 도달 예정 DSP: {arrivalDspTime:F3} / 현재 DSP: {AudioSettings.dspTime:F3}");
    }

    void Update()
    {
        if (!isInitialized) return;

        if (!hasJudged)
        {
            float t = (float)((AudioSettings.dspTime - spawnDspTime) / (arrivalDspTime - spawnDspTime));
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(spawnPosition, targetPosition, t);

            if (AudioSettings.dspTime >= arrivalDspTime)
            {
                hasJudged = true;
                Crash();
                EvaluateSpeed();
            }
        }

        if (transform.position.x < -20f)
            Destroy(gameObject);
    }

    void EvaluateSpeed()
    {
        float speed = speedController.GetCurrentSpeed();
        Debug.Log($"충돌 판정 / 현재속도: {speed} / 요구속도: {requiredSpeed}");

        if (speed >= requiredSpeed)
        {
            Debug.Log("통과!");
            spawner.OnObstaclePassed();
        }
        else
        {
            Debug.Log("실패!");
            spawner.OnObstacleFailed();
            Destroy(gameObject);
        }
    }

    private void Crash()
    {
        if (hasCrashed) return;
        hasCrashed = true;

        if (Rocks == null || Rocks.Length == 0)
            Rocks = GetComponentsInChildren<Rigidbody>(true);

        float crashForce = speedController.GetCurrentSpeed() * crashForceMultiplier;

        foreach (var rb in Rocks)
        {
            if (rb == null) continue;

            Vector3 direction = Vector3.right + Random.insideUnitSphere * directionNoise;
            direction.x = Mathf.Max(0f, direction.x);

            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.right;
            else
                direction.Normalize();

            rb.transform.SetParent(null, true);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(direction * crashForce, ForceMode.Impulse);
            rb.WakeUp();
        }
    }

    private void OnDestroy()
    {
        foreach (var rb in Rocks)
        {
            if (rb != null)
                Destroy(rb.gameObject);
        }
    }
}