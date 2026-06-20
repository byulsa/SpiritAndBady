using UnityEngine;
using System.Collections.Generic;

public enum JudgeType
{
    Perfect,
    Good,
    Miss
}

public class Judgement : MonoBehaviour
{
    public JudgeType judgeType = JudgeType.Perfect;

    [Header("Judge Line")]
    public Transform judgeLine;

    public List<Transform> notes = new();

    [Header("Judge Range")]
    public float perfectRange = 0.1f;
    public float goodRange = 0.25f;
    public float missRange = 0.5f;

    public JudgeType Judge(Transform note)
    {
        float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

        if (distance <= perfectRange)
        {
            return JudgeType.Perfect;
        }

        if (distance <= goodRange)
        {
            return JudgeType.Good;
        }

        if (distance <= missRange)
        {
            return JudgeType.Miss;
        }

        // 판정 범위를 벗어났음
        return JudgeType.Miss;
    }

    public bool CanJudge(Transform note)
    {
        float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

        return distance <= missRange;
    }

    public Transform GetClosestNote()
    {
        Transform closestNote = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform note in notes)
        {
            if (note == null)
                continue;

            float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestNote = note;
            }
        }

        return closestNote;
    }

    private void OnDrawGizmos()
    {
        if (judgeLine == null)
        {
            return;
        }

        Vector3 center = judgeLine.position;

        // 높이(세로 길이)
        float height = 1f;

        // Perfect
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            center,
            new Vector3(perfectRange * 2f, height, 0.1f));

        // Good
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            center,
            new Vector3(goodRange * 2f, height, 0.1f));

        // Miss
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            center,
            new Vector3(missRange * 2f, height, 0.1f));
    }
}