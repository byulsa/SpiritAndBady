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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


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

        return JudgeType.Miss;
    }
    public Transform GetClosestNote()
    {
        Transform closestNote = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform note in notes)
        {
            float distance = Mathf.Abs(note.position.x - judgeLine.position.x);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestNote = note;
            }
        }

        return closestNote;
    }
}
