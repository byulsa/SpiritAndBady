using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class MatControll : MonoBehaviour
{
    [SerializeField] private Material irisMaterial;

    [Header("Damage Effect")]
    [SerializeField] private float hitRadius = 0f;       // 피해 시
    [SerializeField] private float normalRadius = 1f;    // 평상시

    [SerializeField] private float shrinkTime = 0.05f;   // 빠르게 닫힘
    [SerializeField] private float recoverTime = 0.3f;   // 천천히 복귀

    private Coroutine damageCoroutine;
    private Material irisMaterialInstance;
    private Graphic irisGraphic;

    private static readonly int Radius = Shader.PropertyToID("_Radius");

    private void Awake()
    {
        irisGraphic = GetComponent<Graphic>();

        if (irisMaterial == null)
        {
            Debug.LogError("Iris Material이 지정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        irisMaterialInstance = Instantiate(irisMaterial);
        irisGraphic.material = irisMaterialInstance;
    }

    private void Start()
    {
        irisMaterialInstance.SetFloat(Radius, normalRadius);
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

            irisMaterialInstance.SetFloat(
                Radius,
                Mathf.Lerp(normalRadius, hitRadius, t));

            timer += Time.deltaTime;
            yield return null;
        }

        irisMaterialInstance.SetFloat(Radius, hitRadius);

        // 0 -> 1 (천천히)
        timer = 0f;

        while (timer < recoverTime)
        {
            float t = timer / recoverTime;

            irisMaterialInstance.SetFloat(
                Radius,
                Mathf.Lerp(hitRadius, normalRadius, t));

            timer += Time.deltaTime;
            yield return null;
        }

        irisMaterialInstance.SetFloat(Radius, normalRadius);

        damageCoroutine = null;
    }

    private void OnDestroy()
    {
        if (irisMaterialInstance != null)
        {
            Destroy(irisMaterialInstance);
        }
    }
}
