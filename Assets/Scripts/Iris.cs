using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class Iris : MonoBehaviour
{
    [SerializeField] private Material irisMaterial;
    [SerializeField] private HealthManager healthManager;

    [Header("Iris Effect")]
    [SerializeField] private float openRadius = 1f;
    [SerializeField] private float closedRadius = 0f;
    [SerializeField] private float closeDuration = 1.5f;

    private Material irisMaterialInstance;
    private Graphic irisGraphic;
    private Coroutine deathCoroutine;

    private static readonly int Radius = Shader.PropertyToID("_Radius");

    private void Awake()
    {
        irisGraphic = GetComponent<Graphic>();

        Material sourceMaterial = irisMaterial != null
            ? irisMaterial
            : irisGraphic.material;

        if (sourceMaterial == null)
        {
            Debug.LogError("Iris Material이 지정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        irisMaterialInstance = Instantiate(sourceMaterial);
        irisGraphic.material = irisMaterialInstance;
        irisMaterialInstance.SetFloat(Radius, openRadius);

        if (healthManager == null)
        {
            healthManager = FindAnyObjectByType<HealthManager>();
        }
    }

    private void OnEnable()
    {
        if (healthManager != null)
        {
            healthManager.OnDead += HandleDead;
        }
    }

    private void OnDisable()
    {
        if (healthManager != null)
        {
            healthManager.OnDead -= HandleDead;
        }
    }

    private void HandleDead()
    {
        if (deathCoroutine == null)
        {
            deathCoroutine = StartCoroutine(DeathRoutine());
        }
    }

    private IEnumerator DeathRoutine()
    {
        float timer = 0f;
        float duration = Mathf.Max(0f, closeDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            irisMaterialInstance.SetFloat(
                Radius,
                Mathf.Lerp(openRadius, closedRadius, smoothProgress));

            yield return null;
        }

        irisMaterialInstance.SetFloat(Radius, closedRadius);
        RestartScene();
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (irisMaterialInstance != null)
        {
            Destroy(irisMaterialInstance);
        }
    }
}
