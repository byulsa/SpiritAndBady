using System.Collections;
using UnityEngine;

public class ParabolaMover : MonoBehaviour
{
    public IEnumerator Move(
        Vector3 start,
        Vector3 end,
        Vector3 bulgeDirection,
        float height,
        float duration, Judgement judgement)
    {
        Vector3 middle = (start + end) * 0.5f;
        Vector3 control = middle + bulgeDirection.normalized * height;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 필요하면 부드러운 가감속
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = CalculateBezier(start, control, end, t);
            yield return null;
        }

        transform.position = end;
        judgement.ExFireEx();
        PooledNote.ReturnOrDestroy(gameObject);
    }

    private Vector3 CalculateBezier(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float t)
    {
        float oneMinusT = 1f - t;

        return oneMinusT * oneMinusT * start
             + 2f * oneMinusT * t * control
             + t * t * end;
    }
}
