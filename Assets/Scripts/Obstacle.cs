using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("장애물 설정")]
    public float requiredSpeed = 50f;

    private BackgroundLoop backgroundLoop;
    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;
    private double arrivalDspTime;
    private bool isInitialized = false;
    private bool hasJudged = false;

    void Start()
    {
        backgroundLoop = Object.FindAnyObjectByType<BackgroundLoop>();
        speedController = Object.FindAnyObjectByType<TrainSpeedController>();
    }

    public void Init(ObstacleSpawner obstacleSpawner, double arrivalTime)
    {
        spawner = obstacleSpawner;
        arrivalDspTime = arrivalTime;
        isInitialized = true;
        Debug.Log($"장애물 생성 / 도달 예정 DSP: {arrivalDspTime:F3} / 현재 DSP: {AudioSettings.dspTime:F3}");
    }

    void Update()
    {
        if (!isInitialized || backgroundLoop == null) return;

        transform.Translate(Vector3.left * backgroundLoop.currentSpeed * Time.deltaTime);

        if (!hasJudged && AudioSettings.dspTime >= arrivalDspTime)
        {
            hasJudged = true;
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
}