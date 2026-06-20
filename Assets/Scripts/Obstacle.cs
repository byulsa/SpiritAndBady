using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("ï¿½ï¿½Ö¹ï¿½ ï¿½ï¿½ï¿½ï¿½")]
    public float requiredSpeed = 50f;

    private BackgroundLoop backgroundLoop;
    private TrainSpeedController speedController;
    private ObstacleSpawner spawner;
    private Vector3 targetPosition; // ±âÂ÷ À§Ä¡
    private double arriveTime;      // µµ´Þ DSP Å¸ÀÓ
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

        // ¼Óµµ ±â¹ÝÀ¸·Î ½ºÆù À§Ä¡ ¿ª»ê
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