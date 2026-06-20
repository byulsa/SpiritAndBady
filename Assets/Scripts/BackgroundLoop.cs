using UnityEngine;
using System;

public class BackgroundLoop : MonoBehaviour
{
    public float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float offset = 0f;
    [SerializeField] private float SpeedMultiplier = 0.5f;
    private Material mat;
    private bool hasReachedZero = false;

    public event Action OnSpeedReachedZero;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 3f);
        offset += currentSpeed * Time.deltaTime * 0.01f;
        mat.mainTextureOffset = new Vector2(offset, 0f);

        if (!hasReachedZero && targetSpeed == 0f && currentSpeed < 0.01f)
        {
            hasReachedZero = true;
            currentSpeed = 0f;
            OnSpeedReachedZero?.Invoke();
        }
    }

    public void SetSpeed(float speed)
    {
        targetSpeed = speed * SpeedMultiplier;
        if (speed > 0f) hasReachedZero = false;
    }
}