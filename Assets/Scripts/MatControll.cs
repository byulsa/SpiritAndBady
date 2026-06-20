using System.Collections;
using UnityEngine;

public class MatControll : MonoBehaviour
{
    [SerializeField] private Material irisMaterial;

    [Header("Damage Effect")]
    [SerializeField] private float hitRadius = 0f;       // 피해 시
    [SerializeField] private float normalRadius = 1f;    // 평상시

    [SerializeField] private float shrinkTime = 0.05f;   // 빠르게 닫힘
    [SerializeField] private float recoverTime = 0.3f;   // 천천히 복귀

    private Coroutine damageCoroutine;

    private void Start()
    {
        irisMaterial.SetFloat("_Radius", normalRadius);
    }

    public void DamageEffect()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }

        damageCoroutine = StartCoroutine(DamageEffectCoroutine());
    }

    private IEnumerator DamageEffectCoroutine()
    {
        float timer = 0f;

        // 1 -> 0 (빠르게)
        while (timer < shrinkTime)
        {
            float t = timer / shrinkTime;

            irisMaterial.SetFloat(
                "_Radius",
                Mathf.Lerp(normalRadius, hitRadius, t));

            timer += Time.deltaTime;
            yield return null;
        }

        irisMaterial.SetFloat("_Radius", hitRadius);

        // 0 -> 1 (천천히)
        timer = 0f;

        while (timer < recoverTime)
        {
            float t = timer / recoverTime;

            irisMaterial.SetFloat(
                "_Radius",
                Mathf.Lerp(hitRadius, normalRadius, t));

            timer += Time.deltaTime;
            yield return null;
        }

        irisMaterial.SetFloat("_Radius", normalRadius);

        damageCoroutine = null;
    }
}