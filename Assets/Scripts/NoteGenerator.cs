using System.Collections.Generic;
using UnityEngine;
using System;

public class NoteGenerator : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private Judgement judgement;

    [Header("SFX")]
    public AudioSource Audio;
    public AudioClip SFX;

    [Header("Note")]
    public GameObject NotePrefab;
    public Transform SpawnPoint;
    public Transform JudgementPoint;
    [SerializeField, Min(0f)] private float audioScheduleAheadTime = 0.1f;

    [Header("Note Pool")]
    [SerializeField, Min(0)] private int initialPoolSize = 16;
    [SerializeField, Min(1)] private int maxPoolSize = 32;

    private const int MeasuresPerWave = 4;
    private readonly List<NoteTiming> currentNoteTimings = new List<NoteTiming>();
    private NotePool notePool;
    private MeasureData[] currentWave;
    private int waveStartMeasureIndex;
    private int currentWaveMeasureIndex;
    private int nextNoteIndex;
    private WavePhase phase;
    private bool isWavePlaying;
    private bool isSubscribed;

    public event Action OnWaveFinished;
    public event Action<int> OnMeasureStarted;
    public event Action<MeasureData[]> OnWaveStarted;

    private void Awake()
    {
        EnsureNotePool();
    }

    private void OnEnable()
    {
        FindRythmManager();

        SubscribeToRythmManager();
    }

    private void OnDisable()
    {
        if (rythmManager != null && isSubscribed)
        {
            rythmManager.OnMeasureStart -= HandleMeasureStart;
            isSubscribed = false;
        }
    }

    private void Update()
    {
        if (!isWavePlaying || phase != WavePhase.Guide)
        {
            return;
        }
        GenerateDueNotes(
            (float)rythmManager.CurrentMeasureElapsedTime + audioScheduleAheadTime);
    }

    public void WaveStart(MeasureData[] input)
    {
        FindRythmManager();
        SubscribeToRythmManager();

        if (rythmManager == null)
        {
            Debug.LogError("RythmManager is required to start a wave.");
            return;
        }

        if (!rythmManager.IsRunning || rythmManager.CurrentMeasureIndex < 0)
        {
            Debug.LogError("Start the RythmManager clock before starting a wave.");
            return;
        }

        if (input == null || input.Length != MeasuresPerWave)
        {
            Debug.LogError($"WaveStart requires exactly {MeasuresPerWave} MeasureData objects.");
            return;
        }

        currentWave = (MeasureData[])input.Clone();
        OnWaveStarted?.Invoke(currentWave);

        waveStartMeasureIndex = rythmManager.CurrentMeasureIndex;
        currentWaveMeasureIndex = 0;

        phase = WavePhase.Guide;
        isWavePlaying = true;

        BuildCurrentMeasureTimings();
        OnMeasureStarted?.Invoke(0);

        GenerateDueNotes(
            (float)rythmManager.CurrentMeasureElapsedTime + audioScheduleAheadTime);
        Debug.Log($"Wave Start: rhythm measure {waveStartMeasureIndex + 1}");
    }

    private void HandleMeasureStart(int rhythmMeasureIndex)
    {
        if (!isWavePlaying)
        {
            return;
        }
        int waveMeasureOffset = rhythmMeasureIndex - waveStartMeasureIndex;

        if (waveMeasureOffset <= 0)
        {
            return;
        }

        if (waveMeasureOffset >= MeasuresPerWave * 2)
        {
            isWavePlaying = false;
            Debug.Log("Wave Complete");
            OnWaveFinished?.Invoke();
            return;
        }

        if (waveMeasureOffset % 2 == 0)
        {
            currentWaveMeasureIndex = waveMeasureOffset / 2;
            phase = WavePhase.Guide;
            BuildCurrentMeasureTimings();

            OnMeasureStarted?.Invoke(currentWaveMeasureIndex);
            GenerateDueNotes(audioScheduleAheadTime);
            Debug.Log($"Guide Start: measure {currentWaveMeasureIndex + 1}");
            return;
        }

        phase = WavePhase.Judgement;
        Debug.Log($"Judgement Start: measure {currentWaveMeasureIndex + 1}");
    }

    private void GenerateDueNotes(float measureElapsedTime)
    {
        while (nextNoteIndex < currentNoteTimings.Count &&
               currentNoteTimings[nextNoteIndex].time <= measureElapsedTime)
        {
            GenerateNote(currentNoteTimings[nextNoteIndex]);
            nextNoteIndex++;
        }
    }

    private void GenerateNote(NoteTiming noteTiming)
    {
        if (NotePrefab != null && SpawnPoint != null && JudgementPoint != null &&
            EnsureNotePool())
        {
            PooledNote pooledNote = notePool.Get(SpawnPoint.position, SpawnPoint.rotation);
            GameObject note = pooledNote.gameObject;
            if (judgement != null)
            {
                judgement.RegisterNote(note.transform);
            }

            TimedNoteMover mover = pooledNote.Mover;

            double guideDspTime = GetCurrentMeasureDspTime(noteTiming.beatPosition);
            double judgementDspTime =
                rythmManager.GetNextMeasureDspTime(noteTiming.beatPosition);

            mover.Initialize(
                SpawnPoint.position,
                JudgementPoint.position,
                guideDspTime,
                judgementDspTime);

            ScheduleSfx(guideDspTime);
        }
    }

    private bool EnsureNotePool()
    {
        if (notePool != null && notePool.IsInitialized)
        {
            return true;
        }

        if (NotePrefab == null)
        {
            return false;
        }

        notePool = GetComponent<NotePool>();
        if (notePool == null)
        {
            notePool = gameObject.AddComponent<NotePool>();
        }

        notePool.Initialize(NotePrefab, initialPoolSize, maxPoolSize);
        return notePool.IsInitialized;
    }

    private double GetCurrentMeasureDspTime(float beatPosition)
    {
        return rythmManager.CurrentMeasureStartDspTime +
               beatPosition * rythmManager.SecondsPerBeat;
    }

    private void ScheduleSfx(double dspTime)
    {
        if (Audio == null || SFX == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("Scheduled Note SFX");
        audioObject.transform.SetParent(transform, false);

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = Audio.outputAudioMixerGroup;
        source.volume = Audio.volume;
        source.pitch = Audio.pitch;
        source.panStereo = Audio.panStereo;
        source.spatialBlend = Audio.spatialBlend;
        source.reverbZoneMix = Audio.reverbZoneMix;
        source.rolloffMode = Audio.rolloffMode;
        source.minDistance = Audio.minDistance;
        source.maxDistance = Audio.maxDistance;
        source.clip = SFX;

        double now = AudioSettings.dspTime;
        if (dspTime > now)
        {
            source.PlayScheduled(dspTime);
        }
        else
        {
            // 이미 지난 DSP 시각은 예약할 수 없으므로 가능한 즉시 재생한다.
            source.Play();
        }

        float pitch = Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        float delay = (float)System.Math.Max(0d, dspTime - now) +
                      SFX.length / pitch + 0.1f;
        Destroy(audioObject, delay);
    }

    private void BuildCurrentMeasureTimings()
    {
        currentNoteTimings.Clear();
        nextNoteIndex = 0;

        MeasureData measure = currentWave[currentWaveMeasureIndex];

        if (measure == null || measure.beats == null)
        {
            return;
        }

        int beatCount = Mathf.Min(RythmManager.BeatsPerMeasure, measure.beats.Length);

        for (int beatIndex = 0; beatIndex < beatCount; beatIndex++)
        {
            BeatData beat = measure.beats[beatIndex];

            if (beat == null || beat.noteTicks == null || beat.subdivisions <= 0)
            {
                continue;
            }

            for (int noteIndex = 0; noteIndex < beat.noteTicks.Length; noteIndex++)
            {
                int tick = beat.noteTicks[noteIndex];
                float beatPosition = beatIndex + (float)tick / beat.subdivisions;
                float noteTime = beatPosition * rythmManager.SecondsPerBeat;

                currentNoteTimings.Add(
                    new NoteTiming(beatIndex, tick, beatPosition, noteTime));
            }
        }

        currentNoteTimings.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void FindRythmManager()
    {
        if (rythmManager == null)
        {
            rythmManager = FindAnyObjectByType<RythmManager>();
        }
    }

    private void SubscribeToRythmManager()
    {
        if (rythmManager == null || isSubscribed)
        {
            return;
        }
        rythmManager.OnMeasureStart += HandleMeasureStart;
        isSubscribed = true;
    }

    private void OnValidate()
    {
        initialPoolSize = Mathf.Max(0, initialPoolSize);
        maxPoolSize = Mathf.Max(1, initialPoolSize, maxPoolSize);
    }

    private enum WavePhase
    {
        Guide,
        Judgement
    }
}

public readonly struct NoteTiming
{
    public readonly int beatIndex;
    public readonly int tick;
    public readonly float beatPosition;
    public readonly float time;

    public NoteTiming(int beatIndex, int tick, float beatPosition, float time)
    {
        this.beatIndex = beatIndex;
        this.tick = tick;
        this.beatPosition = beatPosition;
        this.time = time;
    }
}
