using UnityEngine;

public class NoteGenerator : MonoBehaviour
{
    [Header("Timing")]
    public float BPM = 120f;
    public float StartDelay = 2f;

    [Header("SFX")]
    public AudioSource Audio;
    public AudioClip SFX;

    [Header("Chart")]
    public MeasureData[] measures;

    private int nextNoteIndex;
    private NoteTiming[] noteTimings;
    private float timer;

    private const int BeatsPerMeasure = 4;

    private void Awake()
    {
        BuildNoteTimings();
    }

    private void OnEnable()
    {
        nextNoteIndex = 0;
        timer = 0f;
    }

    void Update()
    {
        if (noteTimings == null || nextNoteIndex >= noteTimings.Length)
        {
            return;
        }

        timer += Time.deltaTime;
        float chartTime = timer - StartDelay;

        if (chartTime < 0f)
        {
            return;
        }

        while (nextNoteIndex < noteTimings.Length && chartTime >= noteTimings[nextNoteIndex].time)
        {
            GenerateNote(noteTimings[nextNoteIndex]);
            nextNoteIndex++;
        }
    }

    private void GenerateNote(NoteTiming noteTiming)
    {
        if (Audio != null && SFX != null)
        {
            Audio.PlayOneShot(SFX);
        }
        Debug.Log($"Note: measure {noteTiming.measureIndex + 1}, beat {noteTiming.beatIndex + 1}, tick {noteTiming.tick}, World Time {timer}");
    }

    private void BuildNoteTimings()
    {
        if (measures == null)
        {
            noteTimings = new NoteTiming[0];
            return;
        }

        int noteCount = CountNotes();
        noteTimings = new NoteTiming[noteCount];

        int index = 0;

        for (int measureIndex = 0; measureIndex < measures.Length; measureIndex++)
        {
            MeasureData measure = measures[measureIndex];

            if (measure == null || measure.beats == null)
            {
                continue;
            }

            for (int beatIndex = 0; beatIndex < Mathf.Min(BeatsPerMeasure, measure.beats.Length); beatIndex++)
            {
                BeatData beat = measure.beats[beatIndex];

                if (beat == null || beat.noteTicks == null || beat.subdivisions <= 0)
                {
                    continue;
                }

                for (int noteIndex = 0; noteIndex < beat.noteTicks.Length; noteIndex++)
                {
                    int tick = beat.noteTicks[noteIndex];
                    float time = GetNoteTime(measureIndex, beatIndex, tick, beat.subdivisions);

                    noteTimings[index] = new NoteTiming(measureIndex, beatIndex, tick, time);
                    index++;
                }
            }
        }

        System.Array.Sort(noteTimings, (a, b) => a.time.CompareTo(b.time));
    }

    private int CountNotes()
    {
        int count = 0;

        foreach (MeasureData measure in measures)
        {
            if (measure == null || measure.beats == null)
            {
                continue;
            }

            foreach (BeatData beat in measure.beats)
            {
                if (beat == null || beat.noteTicks == null || beat.subdivisions <= 0)
                {
                    continue;
                }

                count += beat.noteTicks.Length;
            }
        }

        return count;
    }

    private float GetNoteTime(int measureIndex, int beatIndex, int tick, int subdivisions)
    {
        float secondsPerBeat = 60f / BPM;
        float measureStartTime = measureIndex * GetMeasureLength();
        float beatStartTime = beatIndex * secondsPerBeat;
        float tickOffsetTime = ((float)tick / subdivisions) * secondsPerBeat;

        return measureStartTime + beatStartTime + tickOffsetTime;
    }

    private float GetMeasureLength()
    {
        return 60f / BPM * BeatsPerMeasure;
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
}
public struct NoteTiming
{
    public int measureIndex;
    public int beatIndex;
    public int tick;
    public float time;
    public NoteTiming(int measureIndex, int beatIndex, int tick, float time)
    {
        this.measureIndex = measureIndex;
        this.beatIndex = beatIndex;
        this.tick = tick;
        this.time = time;
    }
}
