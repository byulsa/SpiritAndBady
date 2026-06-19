using System;
using System.Collections.Generic;
using UnityEngine;

public class RythmManager : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay;
    public const int BeatsPerMeasure = 4;

    public event Action<int> OnBeat;
    public event Action<int> OnMeasureStart;

    public float BPM => currentBpm;
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
    }

    public void StopClock(bool clearScheduledActions = false)
    {
        IsRunning = false;
        CurrentBeatIndex = -1;
        CurrentMeasureIndex = -1;

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

        pendingBpm = newBpm;
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
        if (!IsRunning || CurrentMeasureIndex < 0)
        {
            return AudioSettings.dspTime;
        }

        float nextMeasureBpm = pendingBpm ?? currentBpm;
        double nextMeasureStart = CurrentMeasureStartDspTime + SecondsPerMeasure;
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

            ApplyPendingBpm();
            ExecuteScheduledActions();
            OnMeasureStart?.Invoke(CurrentMeasureIndex);
        }

        OnBeat?.Invoke(CurrentBeatIndex);
        nextBeatDspTime = beatDspTime + SecondsPerBeat;
    }

    private void ApplyPendingBpm()
    {
        if (!pendingBpm.HasValue)
        {
            return;
        }

        currentBpm = pendingBpm.Value;
        bpm = currentBpm;
        pendingBpm = null;
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

        if (startDelay < 0f)
        {
            startDelay = 0f;
        }
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
