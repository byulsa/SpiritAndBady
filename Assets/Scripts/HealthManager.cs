using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HealthManager : MonoBehaviour
{
    [Header("HP 설정")]
    public int maxHP = 5;
    private int currentHP;

    [Header("이펙트")]
    public GameObject damageEffect;
    public AudioClip damageSound;
    public GameObject gameOverEffect;

    [SerializeField] private TrainSpeedController trainSpeedController;
    [SerializeField] private BackgroundLoop backgroundLoop;

    private AudioSource audioSource;

    public event Action<int> OnHPChanged;
    public event Action OnDead;

    void Start()
    {
        currentHP = maxHP;
        audioSource = GetComponent<AudioSource>();
        OnHPChanged?.Invoke(currentHP);
    }

    private void Update()
    {
        if (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.R))
            Restart();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TakeDamage()
    {
        currentHP = Mathf.Max(0, currentHP - 1);
        OnHPChanged?.Invoke(currentHP);
        Debug.Log($"HP: {currentHP}/{maxHP}");

        if (damageEffect != null)
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        if (audioSource != null && damageSound != null)
            audioSource.PlayOneShot(damageSound);

        if (currentHP <= 0)
            GameOver();
    }

    private void GameOver()
    {
        Debug.Log("게임 오버");

        if (gameOverEffect != null)
            Instantiate(gameOverEffect, transform.position, Quaternion.identity);

        if (trainSpeedController != null)
            trainSpeedController.SetSpeedZero();

        // 속도 0 될 때 멈추도록 구독
        if (backgroundLoop != null)
            backgroundLoop.OnSpeedReachedZero += OnSpeedReachedZero;

        OnDead?.Invoke();
    }

    private void OnSpeedReachedZero()
    {
        Time.timeScale = 0f;
        Debug.Log("속도 0 - 게임 정지");
    }

    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
}