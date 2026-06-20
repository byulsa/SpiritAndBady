using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("장애물 설정")]
    public float requiredSpeed = 50f;

    private BackgroundLoop backgroundLoop;
    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;
    private Vector3 targetPosition; // 기차 위치
    private double arriveTime;      // 도달 DSP 타임
    private bool isInitialized = false;

    void Start()
    {
        backgroundLoop = Object.FindAnyObjectByType<BackgroundLoop>();
        speedController = Object.FindAnyObjectByType<TrainSpeedController>();
    }

    public void Init(ObstacleSpawner obstacleSpawner, Vector3 trainPosition, double arrivalDspTime)
    {
        spawner = obstacleSpawner;
        targetPosition = trainPosition;
        arriveTime = arrivalDspTime;
        isInitialized = true;

        // 속도 기반으로 스폰 위치 역산
        float timeToArrive = (float)(arrivalDspTime - AudioSettings.dspTime);
        float spawnOffsetX = backgroundLoop != null ? backgroundLoop.currentSpeed * timeToArrive : 9f;
        transform.position = new Vector3(targetPosition.x + spawnOffsetX, transform.position.y, transform.position.z);
    }

    void Update()
    {
        if (!isInitialized || backgroundLoop == null) return;

        transform.Translate(Vector3.left * backgroundLoop.currentSpeed * Time.deltaTime);

        if (transform.position.x < -20f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Train"))
        {
            if (speedController.GetCurrentSpeed() >= requiredSpeed)
                spawner.OnObstaclePassed();
            else
                spawner.OnObstacleFailed();
        }
    }
}