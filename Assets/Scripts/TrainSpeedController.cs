using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class TrainSpeedController : MonoBehaviour
{
    [Header("노트 타입별 속도 (km)")]
    [SerializeField] private float[] DifficultySpeed;
    [SerializeField] private float maxSpeed = 1000.0f;
    [SerializeField] private float initialSpeed = 60.0f;
    [SerializeField] private float currentSpeed = 0f;
    public event Action<float> OnSpeedChanged;
    private float TotalExpectedSpeedGain = 0;
    public event Action<float> OnExpectedSpeedGainChanged;
    private MeasureData[] CurrentWave;
    private float decreaseSpeed;
    private int totalNotes = 0;
    private float currentRequiredSpeed = 0f;

    [Header("Components Reference")]
    [SerializeField] private NoteGenerator NoteGenerator;
    [SerializeField] private Judgement judgement;
    [SerializeField] private PatternInput PlayerInput;

    [Header("기차 이동 효과")]
    [SerializeField] private Transform trainTransform;
    [SerializeField] private AudioSource normalAudioSource;
    [SerializeField] private AudioSource chargeAudioSource;
    [SerializeField] private AudioClip chargeSound;
    public float moveDistance = -1f;
    public float moveDuration = 0.5f;
    public float returnDelay = 0.5f;
    private Vector3 trainOriginalPosition;

    private void Awake()
    {
        if (judgement == null)
            judgement = FindAnyObjectByType<Judgement>();
        if (NoteGenerator == null)
            NoteGenerator = FindAnyObjectByType<NoteGenerator>();
        if (PlayerInput == null)
            PlayerInput = FindAnyObjectByType<PatternInput>();
    }

    private void OnEnable()
    {
        if (judgement)
            judgement.OnJudged += HandleJudge;
        if (NoteGenerator)
        {
            NoteGenerator.OnWaveStarted += OnWaveStarted;
            NoteGenerator.OnMeasureStarted += OnMeasureStarted;
            NoteGenerator.OnWaveFinished += OnWaveFinished;
        }
        if (PlayerInput)
            PlayerInput.OnMeasureSelected += OnMeasureSelected;
    }

    private void OnDisable()
    {
        if (judgement != null)
            judgement.OnJudged -= HandleJudge;
        if (NoteGenerator != null)
        {
            NoteGenerator.OnWaveStarted -= OnWaveStarted;
            NoteGenerator.OnMeasureStarted -= OnMeasureStarted;
            NoteGenerator.OnWaveFinished -= OnWaveFinished;
        }
        if (PlayerInput)
            PlayerInput.OnMeasureSelected -= OnMeasureSelected;
    }

    private void Start()
    {
        currentSpeed = initialSpeed;
        OnSpeedChanged?.Invoke(currentSpeed);
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
        if (trainTransform != null)
            trainOriginalPosition = trainTransform.position;
    }

    private void OnMeasureSelected(int _, int Difficulty)
    {
        if (Difficulty < 0 || Difficulty >= DifficultySpeed.Length)
            return;
        TotalExpectedSpeedGain += DifficultySpeed[Difficulty];
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
    }

    private void OnWaveFinished()
    {
        currentSpeed += TotalExpectedSpeedGain;
        TotalExpectedSpeedGain = 0;
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
        OnSpeedChanged?.Invoke(currentSpeed);
    }

    private void OnWaveStarted(MeasureData[] datas)
    {
        if (datas == null) return;
        CurrentWave = datas;
        TotalExpectedSpeedGain = 0;
        foreach (var data in datas)
            TotalExpectedSpeedGain += DifficultySpeed[(int)data.difficulty];
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
    }

    private void OnMeasureStarted(int index)
    {
        if (CurrentWave == null || index >= CurrentWave.Length) return;
        totalNotes = CurrentWave[index].GetNotes();
        decreaseSpeed = DifficultySpeed[(int)CurrentWave[index].difficulty] / (totalNotes > 0 ? totalNotes : 1.0f);
    }

    void HandleJudge(JudgeType result)
    {
        if (result == JudgeType.Miss)
        {
            TotalExpectedSpeedGain -= decreaseSpeed;
            OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
        }
    }
    public void SetRequiredSpeed(float speed)
    {
        currentRequiredSpeed = speed;
    }
    IEnumerator TrainChargeEffect()
    {
        // 평소 사운드 끄고 증기 사운드 켜기
        if (normalAudioSource != null) normalAudioSource.Stop();
        if (chargeAudioSource != null)
        {
            chargeAudioSource.clip = chargeSound;
            chargeAudioSource.Play();
        }

        float elapsed = 0f;
        Vector3 startPos = trainOriginalPosition;
        Vector3 targetPos = trainOriginalPosition + new Vector3(moveDistance * 2.6f, 0f, 0f);
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            trainTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        trainTransform.position = targetPos;
    }

    public void TryChargeEffect()
    {
        if (currentSpeed >= currentRequiredSpeed)
            StartCoroutine(TrainChargeEffect());
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetMaxSpeed() => maxSpeed;
    public Vector3 GetTrainOriginalPosition() => trainOriginalPosition;

    public void ResetSpeed()
    {
        currentSpeed = initialSpeed;
    }

    public void OnObstacleResult(bool passed)
    {
        currentSpeed = initialSpeed;
        OnSpeedChanged?.Invoke(currentSpeed);
        StartCoroutine(ReturnWithDelay());
    }

    IEnumerator ReturnWithDelay()
    {
        yield return new WaitForSeconds(returnDelay);
        StartCoroutine(ReturnToOriginalPosition());
    }

    IEnumerator ReturnToOriginalPosition()
    {
        float elapsed = 0f;
        Vector3 startPos = trainTransform.position;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            trainTransform.position = Vector3.Lerp(startPos, trainOriginalPosition, t);
            yield return null;
        }

        trainTransform.position = trainOriginalPosition;

        // 복귀 후 평소 사운드로 복귀
        if (chargeAudioSource != null) chargeAudioSource.Stop();
        if (normalAudioSource != null) normalAudioSource.Play();
    }

    public void SetSpeedZero()
    {
        currentSpeed = 0f;
        TotalExpectedSpeedGain = 0f;
        OnSpeedChanged?.Invoke(currentSpeed);
        Debug.Log("게임 오버 - 속도 0");
    }
}