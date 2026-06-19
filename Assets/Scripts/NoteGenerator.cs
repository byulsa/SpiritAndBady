using System.Collections.Generic;
using UnityEngine;

public class NoteGenerator : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private RythmManager rythmManager;

    [Header("SFX")]
    public AudioSource Audio;
    public AudioClip SFX;

    [Header("Note")]
    public GameObject NotePrefab;
    public Transform SpawnPoint;
    public Transform JudgementPoint;

    private const int MeasuresPerWave = 4;

    private readonly List<NoteTiming> currentNoteTimings = new List<NoteTiming>();

    private MeasureData[] currentWave;
    private int waveStartMeasureIndex;
    private int currentWaveMeasureIndex;
    private int nextNoteIndex;
    private WavePhase phase;
    private bool isWavePlaying;
    private bool isSubscribed;

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
        GenerateDueNotes((float)rythmManager.CurrentMeasureElapsedTime);
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
        waveStartMeasureIndex = rythmManager.CurrentMeasureIndex;
        currentWaveMeasureIndex = 0;
        phase = WavePhase.Guide;
        isWavePlaying = true;

        BuildCurrentMeasureTimings();
        GenerateDueNotes((float)rythmManager.CurrentMeasureElapsedTime);

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
            return;
        }

        if (waveMeasureOffset % 2 == 0)
        {
            currentWaveMeasureIndex = waveMeasureOffset / 2;
            phase = WavePhase.Guide;
            BuildCurrentMeasureTimings();
            GenerateDueNotes(0f);
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
        if (NotePrefab != null && SpawnPoint != null && JudgementPoint != null)
        {
            GameObject note = Instantiate(NotePrefab, SpawnPoint.position, SpawnPoint.rotation);
            TimedNoteMover mover = note.GetComponent<TimedNoteMover>();

            if (mover == null)
            {
                mover = note.AddComponent<TimedNoteMover>();
            }

            double judgementDspTime = rythmManager.GetNextMeasureDspTime(noteTiming.beatPosition);
            mover.Initialize(SpawnPoint.position, JudgementPoint.position, judgementDspTime);
        }

        if (Audio != null && SFX != null)
        {
            Audio.PlayOneShot(SFX);
        }

        Debug.Log(
            $"Note: measure {currentWaveMeasureIndex + 1}, " +
            $"beat {noteTiming.beatIndex + 1}, tick {noteTiming.tick}");
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
