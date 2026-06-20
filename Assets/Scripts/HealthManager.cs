using System;
using UnityEngine;

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

    private AudioSource audioSource;

    public event Action<int> OnHPChanged;
    public event Action OnDead;

    void Start()
    {
        currentHP = maxHP;
        audioSource = GetComponent<AudioSource>();
        OnHPChanged?.Invoke(currentHP);
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
        {
            Debug.Log("게임 오버");
            if (gameOverEffect != null)
                Instantiate(gameOverEffect, transform.position, Quaternion.identity);

            // 속도 0으로
            if (trainSpeedController != null)
                trainSpeedController.SetSpeedZero();

            OnDead?.Invoke();
        }
    }

    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
}