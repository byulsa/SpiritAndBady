using UnityEngine;

public class TestInput : MonoBehaviour
{
    public Judgement judgement;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // JudgeType result = judgement.JudgeClosestNote();
            // Debug.Log(result);
        }
    }
}
