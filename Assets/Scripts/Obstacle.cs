using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("장애물 설정")]
    public float requiredSpeed ; // 장애물마다 Inspector에서 직접 설정

    private BackgroundLoop backgroundLoop;
    private ObstacleSpawner spawner;

    void Start()
    {
        backgroundLoop = Object.FindFirstObjectByType<BackgroundLoop>();
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
            if (backgroundLoop.currentSpeed >= requiredSpeed)
            {
                spawner.OnObstaclePassed();
                // 통과 연출 추가 가능
            }
            else
            {
                spawner.OnObstacleFailed();
                // healthManager.TakeDamage(); 추후 연동
            }
        }
    }
}