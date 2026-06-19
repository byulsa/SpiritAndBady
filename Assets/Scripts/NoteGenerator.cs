using System.Collections.Generic;
using UnityEngine;

public class NoteGenerator : MonoBehaviour
{
    [Header("Timing")]
    public float BPM = 120f;
    public float StartDelay = 2f;

    [Header("SFX")]
    public AudioSource Audio;
    public AudioClip SFX;

    [Header("Note")]
    public GameObject NotePrefab;
    public Transform SpawnPoint;
    public Transform JudgementPoint;

    private const int BeatsPerMeasure = 4;
    private const int MeasuresPerWave = 4;

    private readonly List<NoteTiming> currentNoteTimings = new List<NoteTiming>();

    private MeasureData[] currentWave;
    private int currentMeasureIndex;
    private int nextNoteIndex;
    private float phaseElapsedTime;
    private float measureLength;
    private float secondsPerBeat;
    private WavePhase phase;
    private bool isWavePlaying;

    public void WaveStart(MeasureData[] input)
    {
        if (input == null || input.Length != MeasuresPerWave)
        {
            Debug.LogError($"WaveStart requires exactly {MeasuresPerWave} MeasureData objects.");
            return;
        }

        if (BPM <= 0f)
        {
            Debug.LogError("BPM must be greater than 0.");
            return;
        }

        currentWave = (MeasureData[])input.Clone();
        secondsPerBeat = 60f / BPM;
        measureLength = secondsPerBeat * BeatsPerMeasure;
        currentMeasureIndex = 0;
        phaseElapsedTime = -StartDelay;
        phase = WavePhase.Guide;
        isWavePlaying = true;

        BuildCurrentMeasureTimings();
        Debug.Log($"Wave Start: BPM {BPM}, Measure Length {measureLength:F3}s");
    }

    private void Update()
    {
        if (!isWavePlaying)
        {
            return;
        }

        phaseElapsedTime += Time.deltaTime;
        while (isWavePlaying)
        {
            if (phase == WavePhase.Guide)
            {
                GenerateDueNotes();

                if (phaseElapsedTime < measureLength)
                {
                    break;
                }

                phaseElapsedTime -= measureLength;
                phase = WavePhase.Judgement;
                Debug.Log($"Judgement Start: measure {currentMeasureIndex + 1}");
                continue;
            }

            if (phaseElapsedTime < measureLength)
            {
                break;
            }

            phaseElapsedTime -= measureLength;
            currentMeasureIndex++;

            if (currentMeasureIndex >= currentWave.Length)
            {
                isWavePlaying = false;
                Debug.Log("Wave Complete");
                break;
            }

            phase = WavePhase.Guide;
            BuildCurrentMeasureTimings();
            Debug.Log($"Guide Start: measure {currentMeasureIndex + 1}");
        }
    }

    private void GenerateDueNotes()
    {
        if (phaseElapsedTime < 0f)
        {
            return;
        }

        while (nextNoteIndex < currentNoteTimings.Count &&
               currentNoteTimings[nextNoteIndex].time <= phaseElapsedTime)
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

            float judgementTime = noteTiming.time + measureLength;
            mover.Initialize(
                SpawnPoint.position,
                JudgementPoint.position,
                phaseElapsedTime,
                judgementTime);
        }

        if (Audio != null && SFX != null)
        {
            Audio.PlayOneShot(SFX);
        }

        Debug.Log(
            $"Note: measure {currentMeasureIndex + 1}, " +
            $"beat {noteTiming.beatIndex + 1}, tick {noteTiming.tick}");
    }

    private void BuildCurrentMeasureTimings()
    {
        currentNoteTimings.Clear();
        nextNoteIndex = 0;

        MeasureData measure = currentWave[currentMeasureIndex];

        if (measure == null || measure.beats == null)
        {
            return;
        }

        int beatCount = Mathf.Min(BeatsPerMeasure, measure.beats.Length);

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
                float tickOffset = (float)tick / beat.subdivisions * secondsPerBeat;
                float noteTime = beatIndex * secondsPerBeat + tickOffset;

                currentNoteTimings.Add(new NoteTiming(beatIndex, tick, noteTime));
            }
        }

        currentNoteTimings.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void OnValidate()
    {
        if (BPM <= 0f)
        {
            BPM = 120f;
        }

        if (StartDelay < 0f)
        {
            StartDelay = 0f;
        }
    }

    private enum WavePhase
    {
        Guide,
        Judgement
    }
}

public struct NoteTiming
{
    public int beatIndex;
    public int tick;
    public float time;

    public NoteTiming(int beatIndex, int tick, float time)
    {
        this.beatIndex = beatIndex;
        this.tick = tick;
        this.time = time;
    }
}
