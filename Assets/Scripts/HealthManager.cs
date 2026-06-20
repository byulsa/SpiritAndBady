using System;
using UnityEngine;


public class HealthManager : MonoBehaviour
{
    [Header("HP ����")]
    public int maxHP = 5;
    private int currentHP;

    [Header("����Ʈ")]
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
            GameOver();
    }

    private void GameOver()
    {
        if (gameOverEffect != null)
            Instantiate(gameOverEffect, transform.position, Quaternion.identity);

        if (trainSpeedController != null)
            trainSpeedController.SetSpeedZero();
        OnDead?.Invoke();
    }
    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
}