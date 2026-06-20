using System;
using Unity.VisualScripting;
using UnityEngine;

public class TrainSpeedController : MonoBehaviour
{
    [Header("��Ʈ Ÿ�Ժ� �ӵ� (km)")]

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

    [Header("Components Reference")]
    [SerializeField] private NoteGenerator NoteGenerator;
    [SerializeField] private BackgroundLoop backgroundLoop;
    [SerializeField] private Judgement judgement;
    [SerializeField] private PatternInput PlayerInput;

    private void Awake()
    {
        if (backgroundLoop == null)
        {
            backgroundLoop = FindAnyObjectByType<BackgroundLoop>();
        }
        if (judgement == null)
        {
            judgement = FindAnyObjectByType<Judgement>();
        }
        if (NoteGenerator == null)
        {
            NoteGenerator = FindAnyObjectByType<NoteGenerator>();
        }
        if (PlayerInput == null)
        {
            PlayerInput = FindAnyObjectByType<PatternInput>();
        }
    }
    private void OnEnable()
    {
        if (judgement)
        {
            judgement.OnJudged += HandleJudge;
        }
        if (NoteGenerator)
        {
            NoteGenerator.OnWaveStarted += OnWaveStarted;
            NoteGenerator.OnMeasureStarted += OnMeasureStarted;
            NoteGenerator.OnWaveFinished += OnWaveFinished;
        }
        if (PlayerInput)
        {
            PlayerInput.OnMeasureSelected += OnMeasureSelected;
        }
    }
    private void OnMeasureSelected(int _, int Difficulty)
    {
        if (Difficulty < 0 || Difficulty >= DifficultySpeed.Length)
        {
            return;
        }
        TotalExpectedSpeedGain += DifficultySpeed[Difficulty];
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
    }
    private void Start()
    {
        currentSpeed = initialSpeed;
        OnSpeedChanged?.Invoke(currentSpeed);
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
        Debug.Log($"backgroundLoop null: {backgroundLoop == null}");
        if (backgroundLoop != null)
            backgroundLoop.SetSpeed(currentSpeed);
    }
    private void OnDisable()
    {
        if (judgement != null)
        {
            judgement.OnJudged -= HandleJudge;
        }
        if (NoteGenerator != null)
        {
            NoteGenerator.OnWaveStarted -= OnWaveStarted;
            NoteGenerator.OnMeasureStarted -= OnMeasureStarted;
            NoteGenerator.OnWaveFinished -= OnWaveFinished;
        }
        if (PlayerInput)
        {
            PlayerInput.OnMeasureSelected -= OnMeasureSelected;
        }
    }
    private void OnWaveFinished()
    {
        currentSpeed += TotalExpectedSpeedGain;
        TotalExpectedSpeedGain = 0;
        OnSpeedChanged?.Invoke(currentSpeed);
        if (backgroundLoop != null)
            backgroundLoop.SetSpeed(currentSpeed);
    }
    private void OnWaveStarted(MeasureData[] datas)
    {
        if (datas == null)
        {
            return;
        }
        CurrentWave = datas;
        TotalExpectedSpeedGain = 0;
        foreach (var data in datas)
        {
            TotalExpectedSpeedGain += DifficultySpeed[(int)data.difficulty];
        }
        OnExpectedSpeedGainChanged?.Invoke(TotalExpectedSpeedGain);
    }
    private void OnMeasureStarted(int index)
    {
        if (CurrentWave == null || index >= CurrentWave.Length)
        {
            return;
        }
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
    public float GetCurrentSpeed() => currentSpeed;
    public float GetMaxSpeed() => maxSpeed;
    public void ResetSpeed()
    {
        currentSpeed = initialSpeed;
        maxSpeed = 0f;
        backgroundLoop.SetSpeed(currentSpeed);
    }

    public void OnObstacleResult(bool passed)
    {
        currentSpeed = initialSpeed;
        OnSpeedChanged?.Invoke(currentSpeed);
        if (backgroundLoop != null)
            backgroundLoop.SetSpeed(currentSpeed);
        Debug.Log($"장애물 결과: {(passed ? "통과" : "실패")} / 속도 초기화: {currentSpeed}");
    }

    public void SetSpeedZero()
    {
        currentSpeed = 0f;
        TotalExpectedSpeedGain = 0f;
        OnSpeedChanged?.Invoke(currentSpeed);
        if (backgroundLoop != null)
            backgroundLoop.SetSpeed(0f);
        Debug.Log("게임 오버 - 속도 0");
    }
}
