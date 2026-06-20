using UnityEngine;

public class TimedNoteMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 judgementPosition;
    private Vector3 destinationPosition;
    private double moveStartTime;
    private double judgementTime;
    private double destinationTime;
    private bool isInitialized;

    public void Initialize(
        Vector3 startPosition,
        Vector3 judgementPosition,
        Vector3 destinationPosition,
        double startDspTime,
        double judgementDspTime)
    {
        this.startPosition = startPosition;
        this.judgementPosition = judgementPosition;
        this.destinationPosition = destinationPosition;
        moveStartTime = startDspTime;
        judgementTime = System.Math.Max(moveStartTime, judgementDspTime);

        double approachDuration = judgementTime - moveStartTime;
        float approachDistance = Vector3.Distance(startPosition, judgementPosition);
        float exitDistance = Vector3.Distance(judgementPosition, destinationPosition);

        if (approachDuration <= 0d || approachDistance <= Mathf.Epsilon)
        {
            destinationTime = judgementTime;
        }
        else
        {
            double unitsPerSecond = approachDistance / approachDuration;
            destinationTime = judgementTime + exitDistance / unitsPerSecond;
        }

        transform.position = startPosition;
        isInitialized = true;
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

        if (destinationTime <= judgementTime || now >= destinationTime)
        {
            transform.position = destinationPosition;
            Destroy(gameObject);
            return;
        }

        float exitT = (float)((now - judgementTime) /
                              (destinationTime - judgementTime));
        transform.position = Vector3.Lerp(
            judgementPosition, destinationPosition, Mathf.Clamp01(exitT));
    }
}
