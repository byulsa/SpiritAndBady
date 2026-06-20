using UnityEngine;

public class TimedNoteMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 judgementPosition;
    private Vector3 movementVelocity;
    private double moveStartTime;
    private double judgementTime;
    private bool isInitialized;

    public void Initialize(
        Vector3 startPosition,
        Vector3 judgementPosition,
        double startDspTime,
        double judgementDspTime)
    {
        this.startPosition = startPosition;
        this.judgementPosition = judgementPosition;
        moveStartTime = startDspTime;
        judgementTime = System.Math.Max(moveStartTime, judgementDspTime);

        double approachDuration = judgementTime - moveStartTime;
        movementVelocity = approachDuration > 0d
            ? (judgementPosition - startPosition) / (float)approachDuration
            : Vector3.zero;

        transform.position = startPosition;
        isInitialized = true;
    }

    public void StopMovement()
    {
        isInitialized = false;
        enabled = false;
    }

    public bool HasPassedJudgementPoint(float distance)
    {
        if (!isInitialized || AudioSettings.dspTime < judgementTime)
        {
            return false;
        }

        Vector3 direction = judgementPosition - startPosition;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }

        float passedDistance = Vector3.Dot(
            transform.position - judgementPosition,
            direction.normalized);
        return passedDistance > distance;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        double now = AudioSettings.dspTime;

        if (now <= moveStartTime)
        {
            transform.position = startPosition;
            return;
        }

        if (now <= judgementTime && judgementTime > moveStartTime)
        {
            float t = (float)((now - moveStartTime) /
                              (judgementTime - moveStartTime));
            transform.position = Vector3.Lerp(
                startPosition, judgementPosition, Mathf.Clamp01(t));
            return;
        }

        float elapsedAfterJudgement = (float)(now - judgementTime);
        transform.position = judgementPosition +
                             movementVelocity * elapsedAfterJudgement;
    }
}
