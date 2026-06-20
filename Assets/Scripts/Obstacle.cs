using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("장애물 설정")]
    public float requiredSpeed = 50f;

    [Header("파괴 설정")]
    [SerializeField] private Rigidbody[] Rocks;
    [SerializeField, Min(0f)] private float crashForceMultiplier = 1f;
    [SerializeField, Range(0f, 2f)] private float directionNoise = 0.75f;

    private BackgroundLoop backgroundLoop;
    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;
    private double arrivalDspTime;
    private bool isInitialized = false;
    private bool hasJudged = false;
    private bool hasCrashed = false;

    void Start()
    {
        backgroundLoop = FindAnyObjectByType<BackgroundLoop>();
        speedController = FindAnyObjectByType<TrainSpeedController>();

        if (Rocks == null || Rocks.Length == 0)
            Rocks = GetComponentsInChildren<Rigidbody>(true);
    }

    public void Init(ObstacleSpawner obstacleSpawner, double arrivalTime,
    BackgroundLoop backgroundLoop, TrainSpeedController speedController)
    {
        this.backgroundLoop = backgroundLoop;
        this.speedController = speedController;
        spawner = obstacleSpawner;
        arrivalDspTime = arrivalTime;
        isInitialized = true;
        Debug.Log($"장애물 생성 / 도달 예정 DSP: {arrivalDspTime:F3} / 현재 DSP: {AudioSettings.dspTime:F3}");
    }

    public float obstacleSpeedScale = 0.1f;
    void Update()
    {
        if (!isInitialized || backgroundLoop == null) return;
        transform.Translate(Vector3.left * backgroundLoop.currentSpeed * obstacleSpeedScale * Time.deltaTime);
        if (!hasJudged && AudioSettings.dspTime >= arrivalDspTime)
        {
            hasJudged = true;
            Crash();
            EvaluateSpeed();
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
        }
    }
    private void Crash()
    {
        if (hasCrashed)
            return;

        hasCrashed = true;

        if (Rocks == null || Rocks.Length == 0)
            Rocks = GetComponentsInChildren<Rigidbody>(true);

        float movingSpeed = backgroundLoop != null
            ? backgroundLoop.currentSpeed * obstacleSpeedScale
            : 0f;
        float crashForce = movingSpeed * crashForceMultiplier;

        foreach (var rb in Rocks)
        {
            if (rb == null)
                continue;

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
            Destroy(rb.gameObject);
        }
    }

}
