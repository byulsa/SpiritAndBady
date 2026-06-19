using UnityEngine;

public class TimedNoteMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private double moveStartTime;
    private double moveEndTime;
    private bool isInitialized;
    
    public void Initialize(Vector3 startPosition, Vector3 targetPosition, float startTime, float arriveTime)
    {
        this.startPosition = startPosition;
        this.targetPosition = targetPosition;
        float remainingTime = Mathf.Max(0f, arriveTime - startTime);
        moveStartTime = Time.timeAsDouble;
        moveEndTime = moveStartTime + remainingTime;

        transform.position = startPosition;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (moveEndTime <= moveStartTime)
        {
            transform.position = targetPosition;
            return;
        }

        float t = (float)((Time.timeAsDouble - moveStartTime) / (moveEndTime - moveStartTime));
        t = Mathf.Clamp01(t);
        transform.position = Vector3.Lerp(startPosition, targetPosition, t);
    }
}
