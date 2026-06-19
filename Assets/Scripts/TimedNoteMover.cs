using UnityEngine;

public class TimedNoteMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private double moveStartTime;
    private double moveEndTime;
    private bool isInitialized;
    public void Initialize(Vector3 startPosition, Vector3 targetPosition, double arriveDspTime)
    {
        this.startPosition = startPosition;
        this.targetPosition = targetPosition;
        moveStartTime = AudioSettings.dspTime;
        moveEndTime = System.Math.Max(moveStartTime, arriveDspTime);

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

        float t = (float)((AudioSettings.dspTime - moveStartTime) / (moveEndTime - moveStartTime));
        t = Mathf.Clamp01(t);
        transform.position = Vector3.Lerp(startPosition, targetPosition, t);
    }
}
