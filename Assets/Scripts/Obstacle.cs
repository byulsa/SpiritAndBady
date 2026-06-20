using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("�ִ� �ӵ�")]
    public float requiredSpeed = 50f;

    private BackgroundLoop backgroundLoop;
    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;
    private Vector3 targetPosition; // ���� ��ġ
    private double arriveTime;      // ���� DSP Ÿ��
    private bool isInitialized = false;

    void Start()
    {
        backgroundLoop = Object.FindAnyObjectByType<BackgroundLoop>();
        speedController = Object.FindAnyObjectByType<TrainSpeedController>();
    }

    public void Init(ObstacleSpawner obstacleSpawner)
    {
        spawner = obstacleSpawner;
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