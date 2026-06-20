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

    [Header("Judge Line")]
    public Transform judgeLine;

    [Header("Judge Range")]
    public float perfectRange = 0.1f;
    public float goodRange = 0.25f;
    public float missRange = 0.5f;

    public JudgeType judgeType = JudgeType.Perfect;

    private readonly List<Transform> notes = new();
    public AudioSource audioSource;

    private void Update()
    {
        CheckMiss();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JudgeCurrentNote();
        }
    }

    public void RegisterNote(Transform note)
    {
        if (note != null)
        {
            notes.Add(note);
        }
    }

    private void CheckMiss()
    {
        Transform note = GetCurrentNote();

        if (note == null)
        {
            return;
        }

        TimedNoteMover mover = note.GetComponent<TimedNoteMover>();

        if (mover != null && mover.HasPassedJudgementPoint(goodRange))
        {
            ResolveNote(note, JudgeType.Miss);
        }
    }

    public void JudgeCurrentNote()
    {
        Transform note = GetCurrentNote();

        if (note == null)
        {
            return;
        }

        if (!CanJudge(note))
        {
            return;
        }

        ResolveNote(note, Judge(note));
    }

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

        return JudgeType.Miss;
    }

    public void ResolveNote(Transform note, JudgeType result)
    {
        judgeType = result;

        OnJudged?.Invoke(result);

        if (note == null)
        {
            return;
        }

        notes.RemoveAt(0);

        Destroy(note.gameObject);
        audioSource.Play();
        Debug.Log($"Judgement : {result}");
    }

    public bool CanJudge(Transform note)
    {
        if (note == null)
        {
            return false;
        }

        float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

        return distance <= missRange;
    }

    private Transform GetCurrentNote()
    {
        while (notes.Count > 0 && notes[0] == null)
        {
            notes.RemoveAt(0);
        }

        if (notes.Count == 0)
        {
            return null;
        }

        return notes[0];
    }

    private void OnDrawGizmos()
    {
        if (judgeLine == null)
        {
            return;
        }

        Vector3 center = judgeLine.position;
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