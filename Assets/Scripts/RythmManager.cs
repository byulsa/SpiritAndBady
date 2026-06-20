using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RythmManager : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private float bpm = 120f;
    [SerializeField, Min(1f)] private float bpmStep = 10f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay;
    public const int BeatsPerMeasure = 4;

    [Header("Test Metronome")]
    [SerializeField] private bool playMetronome;
    [SerializeField] private AudioSource metronomeAudioSource;
    [SerializeField] private AudioClip metronomeClick;
    [Tooltip("Optional click sound for the first beat of each measure.")]
    [SerializeField] private AudioClip metronomeDownbeatClick;
    [SerializeField, Range(0f, 1f)] private float metronomeVolume = 1f;

    public event Action<int> OnBeat;
    public event Action<int> OnMeasureStart;
    public event Action<double, float> OnClockScheduled;
    public event Action OnClockStopped;
    public event Action<float, double, int> OnBpmChangeScheduled;
    public event Action<float, double, int> OnBpmChanged;

    public float BPM => currentBpm;
    public float NextMeasureBPM => pendingBpm ?? currentBpm;
    public float SecondsPerBeat => 60f / currentBpm;
    public float SecondsPerMeasure => SecondsPerBeat * BeatsPerMeasure;
    public int CurrentBeatIndex { get; private set; } = -1;
    public int CurrentMeasureIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; }

    public double CurrentMeasureStartDspTime { get; private set; }

    public double CurrentMeasureElapsedTime
    {
        get
        {
            if (!IsRunning || CurrentMeasureIndex < 0)
            {
                return 0d;
            }

            return Math.Max(0d, AudioSettings.dspTime - CurrentMeasureStartDspTime);
        }
    }

    public float CurrentMeasureProgress =>
        Mathf.Clamp01((float)(CurrentMeasureElapsedTime / SecondsPerMeasure));

    private readonly List<ScheduledMeasureAction> scheduledActions =
        new List<ScheduledMeasureAction>();

    private float currentBpm;
    private float? pendingBpm;
    private double nextBeatDspTime;

    private void Awake()
    {
        currentBpm = bpm;
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartClock(startDelay);
        }
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        double now = AudioSettings.dspTime;

        while (now >= nextBeatDspTime)
        {
            ProcessBeat(nextBeatDspTime);
        }
    }

    public void StartClock(float delay = 0f)
    {
        if (IsRunning)
        {
            return;
        }

        currentBpm = bpm;
        pendingBpm = null;
        CurrentBeatIndex = BeatsPerMeasure - 1;
        CurrentMeasureIndex = -1;
        CurrentMeasureStartDspTime = 0d;
        nextBeatDspTime = AudioSettings.dspTime + Mathf.Max(0f, delay);
        IsRunning = true;

        OnClockScheduled?.Invoke(nextBeatDspTime, currentBpm);
    }

    public void StopClock(bool clearScheduledActions = false)
    {
        IsRunning = false;
        CurrentBeatIndex = -1;
        CurrentMeasureIndex = -1;
        OnClockStopped?.Invoke();

        if (clearScheduledActions)
        {
            scheduledActions.Clear();
        }
    }

    public void ChangeBpmOnNextMeasure(float newBpm)
    {
        if (newBpm <= 0f)
        {
            Debug.LogError("BPM must be greater than 0.");
            return;
        }

        float quantizedBpm = QuantizeBpm(newBpm);

        if (!IsRunning)
        {
            bpm = quantizedBpm;
            currentBpm = quantizedBpm;
            pendingBpm = null;
            return;
        }

        pendingBpm = quantizedBpm;

        int targetMeasureIndex = CurrentMeasureIndex < 0
            ? 0
            : CurrentMeasureIndex + 1;
        double effectiveDspTime = CurrentMeasureIndex < 0
            ? nextBeatDspTime
            : CurrentMeasureStartDspTime + SecondsPerMeasure;

        OnBpmChangeScheduled?.Invoke(
            quantizedBpm,
            effectiveDspTime,
            targetMeasureIndex);
    }

    public void AddBpmOnNextMeasure(float amount)
    {
        ChangeBpmOnNextMeasure(NextMeasureBPM + amount);
    }

    public void RunOnNextMeasure(Action action)
    {
        if (action == null)
        {
            return;
        }

        int targetMeasure = CurrentMeasureIndex < 0
            ? 0
            : CurrentMeasureIndex + 1;

        scheduledActions.Add(new ScheduledMeasureAction(targetMeasure, action));
    }

    public double GetNextMeasureDspTime(float beatPosition)
    {
        if (!IsRunning)
        {
            return AudioSettings.dspTime;
        }

        float nextMeasureBpm = pendingBpm ?? currentBpm;
        double nextMeasureStart = CurrentMeasureIndex < 0
            ? nextBeatDspTime
            : CurrentMeasureStartDspTime + SecondsPerMeasure;
        double nextMeasureSecondsPerBeat = 60d / nextMeasureBpm;

        return nextMeasureStart + beatPosition * nextMeasureSecondsPerBeat;
    }

    private void ProcessBeat(double beatDspTime)
    {
        CurrentBeatIndex++;

        if (CurrentBeatIndex >= BeatsPerMeasure)
        {
            CurrentBeatIndex = 0;
            CurrentMeasureIndex++;
            CurrentMeasureStartDspTime = beatDspTime;

            ApplyPendingBpm(beatDspTime);
            ExecuteScheduledActions();
            OnMeasureStart?.Invoke(CurrentMeasureIndex);
        }

        PlayMetronomeClick();
        OnBeat?.Invoke(CurrentBeatIndex);
        nextBeatDspTime = beatDspTime + SecondsPerBeat;
    }

    private void PlayMetronomeClick()
    {
        if (!playMetronome || metronomeAudioSource == null || metronomeClick == null)
        {
            return;
        }

        AudioClip click = CurrentBeatIndex == 0 && metronomeDownbeatClick != null
            ? metronomeDownbeatClick
            : metronomeClick;

        metronomeAudioSource.PlayOneShot(click, metronomeVolume);
    }

    private void ApplyPendingBpm(double effectiveDspTime)
    {
        if (!pendingBpm.HasValue)
        {
            return;
        }

        currentBpm = pendingBpm.Value;
        bpm = currentBpm;
        pendingBpm = null;

        OnBpmChanged?.Invoke(
            currentBpm,
            effectiveDspTime,
            CurrentMeasureIndex);
    }

    private void ExecuteScheduledActions()
    {
        for (int i = 0; i < scheduledActions.Count;)
        {
            if (scheduledActions[i].measureIndex > CurrentMeasureIndex)
            {
                i++;
                continue;
            }

            Action action = scheduledActions[i].action;
            scheduledActions.RemoveAt(i);
            action.Invoke();
        }
    }

    private void OnValidate()
    {
        if (bpm <= 0f)
        {
            bpm = 120f;
        }

        if (bpmStep < 1f)
        {
            bpmStep = 10f;
        }

        if (startDelay < 0f)
        {
            startDelay = 0f;
        }
    }

    private float QuantizeBpm(float value)
    {
        float step = Mathf.Max(1f, bpmStep);
        return Mathf.Max(step, Mathf.Round(value / step) * step);
    }

    private readonly struct ScheduledMeasureAction
    {
        public readonly int measureIndex;
        public readonly Action action;

        public ScheduledMeasureAction(int measureIndex, Action action)
        {
            this.measureIndex = measureIndex;
            this.action = action;
        }
    }
}
