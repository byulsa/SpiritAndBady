using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(10)]
public class TrainRoad : MonoBehaviour
{
    [Header("Road References")]
    // [FormerlySerializedAs("TrainRoads")]
    // [SerializeField] private Transform[] trainRoads;
    [SerializeField] private Mover[] Elements;
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
    void OnEnable()
    {
        foreach (Mover mover in Elements)
        {
            mover.OnEnd += HandleMoveEnd;
        }
    }
    void OnDisable()
    {
        foreach (Mover mover in Elements)
        {
            mover.OnEnd -= HandleMoveEnd;
        }
    }
    void HandleMoveEnd(Transform Target)
    {
        Target.position = startPosition.position;
    }
    void Update()
    {
        if (!CanMove())
            return;

        Move();
        // CheckEnd();
    }

    private bool CanMove()
    {
        return Elements != null
            && Elements.Length > 0
            && startPosition != null
            && endPosition != null;
    }

    private void Move()
    {
        Vector3 moveDirection = (endPosition.position - startPosition.position).normalized;
        float moveDistance = moveSpeed * Time.deltaTime;

        foreach (var mover in Elements)
        {
            if (mover != null && mover.transform != null)
                mover.transform.position += moveDirection * moveDistance;
        }
    }
}
