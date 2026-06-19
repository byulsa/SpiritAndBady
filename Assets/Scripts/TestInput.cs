using UnityEngine;
using System.Collections.Generic;

public class TestInput : MonoBehaviour
{
    public Judgement judgement;

    public Transform currentNote;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Transform note = judgement.GetClosestNote();

            JudgeType result = judgement.Judge(note);

            Debug.Log($"{result} {note}");
            judgement.notes.Remove(note);
            Destroy(note.gameObject);
        }
    }
}
