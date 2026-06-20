using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(10)]
public class TrainRoad : MonoBehaviour
{
    [Header("Road References")]
    [FormerlySerializedAs("TrainRoads")]
    [SerializeField] private Transform[] trainRoads;
    [FormerlySerializedAs("StartPosition")]
    [SerializeField] private Transform startPosition;
    [FormerlySerializedAs("EndPosition")]
    [SerializeField] private Transform endPosition;

    [Header("Speed")]
    [SerializeField] private float moveSpeed = 5f;

    private int index;

    private void Awake()
    {
        if (startPosition == null)
            startPosition = transform.Find("StartPosition");

        if (endPosition == null)
            endPosition = transform.Find("EndPosition");
    }

    void Update()
    {
        if (!CanMove())
            return;

        Move();
        CheckEnd();
    }

    private bool CanMove()
    {
        return trainRoads != null
            && trainRoads.Length > 0
            && startPosition != null
            && endPosition != null;
    }

    private void CheckEnd()
    {
        Transform currentRoad = trainRoads[index];
        if (currentRoad == null)
        {
            index = (index + 1) % trainRoads.Length;
            return;
        }

        Vector3 moveDirection = (endPosition.position - startPosition.position).normalized;
        bool reachedEnd = Vector3.Dot(
            currentRoad.position - endPosition.position,
            moveDirection
        ) >= 0f;

        if (!reachedEnd)
            return;

        float overshoot = Vector3.Dot(
            currentRoad.position - endPosition.position,
            moveDirection
        );
        currentRoad.position = startPosition.position + moveDirection * overshoot;
        index = (index + 1) % trainRoads.Length;
    }

    private void Move()
    {
        Vector3 moveDirection = (endPosition.position - startPosition.position).normalized;
        float moveDistance = moveSpeed * Time.deltaTime;

        foreach (Transform road in trainRoads)
        {
            if (road != null)
                road.position += moveDirection * moveDistance;
        }
    }
}
