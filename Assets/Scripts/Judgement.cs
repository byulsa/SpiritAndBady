using UnityEngine;
using System.Collections.Generic;
using System;

public enum JudgeType
{
    Perfect,
    Good,
    Miss
}

public class Judgement : MonoBehaviour
{
    public event Action<JudgeType> OnJudged;
    public JudgeType judgeType = JudgeType.Perfect;

    [Header("Judge Line")]
    public Transform judgeLine;

    public List<Transform> notes = new();

    [Header("Judge Range")]
    public float perfectRange = 0.1f;
    public float goodRange = 0.25f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            JudgeClosestNote();
        }

        for (int i = notes.Count - 1; i >= 0; i--)
        {
            Transform note = notes[i];
            if (note == null)
            {
                notes.RemoveAt(i);
                continue;
            }

            TimedNoteMover mover = note.GetComponent<TimedNoteMover>();
            if (mover != null && mover.HasPassedJudgementPoint(goodRange))
            {
                ResolveNote(note, JudgeType.Miss);
            }
        }
    }
    public void RegisterNote(Transform note)
    {
        if (note != null && !notes.Contains(note))
        {
            notes.Add(note);
        }
    }
    public float missRange = 0.5f;

    public JudgeType Judge(Transform note)
    {
        if (note == null || judgeLine == null)
        {
            return JudgeType.Miss;
        }

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

    public void JudgeClosestNote()
    {
        Transform note = GetClosestNote();
        if (note == null)
        {
            return;
        }
        ResolveNote(note, Judge(note));
    }

    public void ResolveNote(Transform note, JudgeType result)
    {
        judgeType = result;
        
        OnJudged?.Invoke(result);

        if (note == null)
        {
            return;
        }
        notes.Remove(note);
        Destroy(note.gameObject);
        Debug.Log($"Judgement: {result}");
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
            {
                continue;
            }

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