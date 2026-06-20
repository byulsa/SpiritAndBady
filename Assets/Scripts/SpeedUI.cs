using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
public class SpeedUI : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private RectTransform TickRect;
    private float MaxFiilValue = 0.75f;
    private float ZeroTickPitch = 270f;
    [SerializeField] private float Duration = 1.5f;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TrainSpeedController Controller;
    private Coroutine gaugeCoroutine;
    private float displayedSpeed;
    void Awake()
    {
        if (Controller == null)
        {
            Controller = FindAnyObjectByType<TrainSpeedController>();
        }
    }
    void OnEnable()
    {
        if (Controller)
        {
            Controller.OnSpeedChanged += OnSpeedChanged;
        }
    }
    void OnDisable()
    {
        if (Controller)
        {
            Controller.OnSpeedChanged -= OnSpeedChanged;
        }

        if (gaugeCoroutine != null)
        {
            StopCoroutine(gaugeCoroutine);
            gaugeCoroutine = null;
        }
    }
    private void OnSpeedChanged(float speed)
    {
        if (Controller)
        {
            if (gaugeCoroutine != null)
            {
                StopCoroutine(gaugeCoroutine);
            }

            gaugeCoroutine = StartCoroutine(SetGage(speed));
        }
    }

    private IEnumerator SetGage(float Speed)
    {
        float maxSpeed = Controller.GetMaxSpeed();
        float normalizedSpeed = maxSpeed > 0f ? Mathf.Clamp01(Speed / maxSpeed) : 0f;

        float StartSpeed = displayedSpeed;
        float StartFillAmount = image ? image.fillAmount : 0f;
        float TargetFillAmount = normalizedSpeed * MaxFiilValue;
        float StartTickPitch = TickRect ? TickRect.localEulerAngles.z : ZeroTickPitch;
        float TargetTickPitch = Mathf.Lerp(ZeroTickPitch, 0f, normalizedSpeed);

        if (Duration <= 0f)
        {
            SetGaugeValues(Speed, TargetFillAmount, TargetTickPitch);
            gaugeCoroutine = null;
            yield break;
        }

        float Timer = 0.0f;
        while (Timer < Duration)
        {
            Timer += Time.deltaTime;
            float progress = Mathf.Clamp01(Timer / Duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            float currentSpeed = Mathf.Lerp(StartSpeed, Speed, easedProgress);
            float fillAmount = Mathf.Lerp(StartFillAmount, TargetFillAmount, easedProgress);
            float tickPitch = Mathf.Lerp(StartTickPitch, TargetTickPitch, easedProgress);
            SetGaugeValues(currentSpeed, fillAmount, tickPitch);

            yield return null;
        }

        SetGaugeValues(Speed, TargetFillAmount, TargetTickPitch);
        gaugeCoroutine = null;
    }

    private void SetGaugeValues(float speed, float fillAmount, float tickPitch)
    {
        displayedSpeed = speed;

        if (speedText)
        {
            speedText.text = speed.ToString("F2") + " Km/h";
        }

        if (image)
        {
            image.fillAmount = fillAmount;
        }

        if (TickRect)
        {
            TickRect.localRotation = Quaternion.Euler(0f, 0f, tickPitch);
        }
    }
}
