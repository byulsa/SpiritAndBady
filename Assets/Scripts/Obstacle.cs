using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("장애물 설정")]
    public float requiredSpeed = 50f;

    private BackgroundLoop backgroundLoop;
    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;

    void Start()
    {
        backgroundLoop = Object.FindFirstAnyType<BackgroundLoop>();
        speedController = Object.FindFirstAnyType<TrainSpeedController>();
    }

    public void Init(ObstacleSpawner obstacleSpawner)
    {
        spawner = obstacleSpawner;
    }

    void Update()
    {
        if (backgroundLoop == null) return;
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