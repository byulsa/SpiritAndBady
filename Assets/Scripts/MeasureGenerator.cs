using System.Collections.Generic;
using UnityEngine;

public class MeasureGenerator : MonoBehaviour
{
    [SerializeField] private RythmManager rythmManager;

    private const int MeasureCount = 4;
    private const int MaxGenerationAttempts = 50;
    private float generationBpm;
    private int nextMeasureIndex;

    public MeasureData[] GeneratedMeasures { get; private set; } =
        new MeasureData[MeasureCount];
    public int GeneratedCount => nextMeasureIndex;
    public bool IsWaveReady => nextMeasureIndex == MeasureCount;

    private static readonly RhythmPattern[][] PatternsByDifficulty =
    {
        new[]
        {
            new RhythmPattern(1, 1),
            new RhythmPattern(1, 9, 0),
            new RhythmPattern(2, 2, 1),
            new RhythmPattern(2, 3, 0, 1)
        },
        new[]
        {
            new RhythmPattern(1, 1),
            new RhythmPattern(1, 4, 0),
            new RhythmPattern(2, 3, 1),
            new RhythmPattern(2, 5, 0, 1),
            new RhythmPattern(4, 4, 0, 2),
            new RhythmPattern(4, 2, 1, 3),
            new RhythmPattern(4, 2, 0, 3),
            new RhythmPattern(4, 1, 0, 1, 3)
        },
        new[]
        {
            new RhythmPattern(1, 1),
            new RhythmPattern(1, 2, 0),
            new RhythmPattern(2, 2, 1),
            new RhythmPattern(2, 3, 0, 1),
            new RhythmPattern(4, 3, 0, 2),
            new RhythmPattern(4, 3, 1, 3),
            new RhythmPattern(4, 3, 0, 1, 3),
            new RhythmPattern(4, 3, 0, 2, 3),
            new RhythmPattern(4, 2, 0, 1, 2, 3),
            new RhythmPattern(8, 1, 0, 2, 4, 6),
            new RhythmPattern(8, 1, 1, 3, 5, 7),
            new RhythmPattern(8, 1, 0, 3, 5, 7)
        }
    };
    public MeasureData MakeMeasure(int difficulty)
    {
        FindRythmManager();

        if (rythmManager == null)
        {
            // Debug.LogError("RythmManager is required to generate measures.");
            return null;
        }

        if (IsWaveReady)
        {
            // Debug.LogError("The wave already has four measures. Call ResetWave first.");
            return null;
        }

        int safeDifficulty = Mathf.Clamp(difficulty, 0, 2);

        if (safeDifficulty != difficulty)
        {
            // Debug.LogWarning("Difficulty must be 0 (Easy), 1 (Normal), or 2 (Hard).");
        }

        generationBpm = rythmManager.NextMeasureBPM;
        bool isFinalMeasure = nextMeasureIndex == MeasureCount - 1;
        BeatData[] beats = GenerateMeasureBeats(safeDifficulty, isFinalMeasure);
        MeasureData measure = CreateMeasureData(beats, nextMeasureIndex, safeDifficulty);

        GeneratedMeasures[nextMeasureIndex] = measure;
        nextMeasureIndex++;

        // Debug.Log(
        //     $"Generated measure {nextMeasureIndex}/{MeasureCount}: " +
        //     $"difficulty {safeDifficulty}, " +
        //     $"BPM {generationBpm:F0}");

        return measure;
    }

    public MeasureData[] GetWaveMeasures()
    {
        if (!IsWaveReady)
        {
            Debug.LogError($"The wave needs four measures. Current count: {nextMeasureIndex}.");
            return null;
        }

        return (MeasureData[])GeneratedMeasures.Clone();
    }

    public void ResetWave()
    {
        ReleasePreviousMeasures();
        GeneratedMeasures = new MeasureData[MeasureCount];
        nextMeasureIndex = 0;
    }

    private BeatData[] GenerateMeasureBeats(int difficulty, bool isFinalMeasure)
    {
        for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            BeatData[] beats = new BeatData[RythmManager.BeatsPerMeasure];

            for (int beatIndex = 0; beatIndex < beats.Length; beatIndex++)
            {
                bool requireDownbeat = beatIndex == 0;
                RhythmPattern pattern = PickPattern(difficulty, requireDownbeat);
                beats[beatIndex] = CreateBeatData(pattern);
            }

            if (isFinalMeasure)
            {
                beats[beats.Length - 1] = CreateCadenceBeat(difficulty);
            }

            if (IsValidMeasure(beats, difficulty))
            {
                return beats;
            }
        }

        return CreateFallbackMeasure(difficulty, isFinalMeasure);
    }

    private RhythmPattern PickPattern(int difficulty, bool requireDownbeat)
    {
        RhythmPattern[] patterns = PatternsByDifficulty[difficulty];
        float minimumInterval = GetMinimumInputInterval(difficulty);
        int totalWeight = 0;

        for (int i = 0; i < patterns.Length; i++)
        {
            if ((!requireDownbeat || patterns[i].ContainsTickZero) &&
                IsPatternPlayable(patterns[i], minimumInterval))
            {
                totalWeight += patterns[i].weight;
            }
        }

        if (totalWeight == 0)
        {
            return new RhythmPattern(1, 1, 0);
        }

        int roll = Random.Range(0, totalWeight);

        for (int i = 0; i < patterns.Length; i++)
        {
            RhythmPattern pattern = patterns[i];

            if ((requireDownbeat && !pattern.ContainsTickZero) ||
                !IsPatternPlayable(pattern, minimumInterval))
            {
                continue;
            }

            if (roll < pattern.weight)
            {
                return pattern;
            }

            roll -= pattern.weight;
        }

        return new RhythmPattern(1, 1, 0);
    }

    private bool IsPatternPlayable(RhythmPattern pattern, float minimumInterval)
    {
        for (int i = 1; i < pattern.ticks.Length; i++)
        {
            float tickDistance =
                (float)(pattern.ticks[i] - pattern.ticks[i - 1]) / pattern.subdivisions;

            if (tickDistance * GetGenerationSecondsPerBeat() < minimumInterval)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsValidMeasure(BeatData[] beats, int difficulty)
    {
        List<float> notePositions = new List<float>();

        for (int beatIndex = 0; beatIndex < beats.Length; beatIndex++)
        {
            BeatData beat = beats[beatIndex];

            for (int tickIndex = 0; tickIndex < beat.noteTicks.Length; tickIndex++)
            {
                int tick = beat.noteTicks[tickIndex];

                if (tick < 0 || tick >= beat.subdivisions)
                {
                    return false;
                }

                notePositions.Add(beatIndex + (float)tick / beat.subdivisions);
            }
        }

        GetNoteCountRange(difficulty, out int minimumNotes, out int maximumNotes);

        if (notePositions.Count < minimumNotes || notePositions.Count > maximumNotes)
        {
            return false;
        }

        notePositions.Sort();
        float minimumInterval = GetMinimumInputInterval(difficulty);

        for (int i = 1; i < notePositions.Count; i++)
        {
            float interval =
                (notePositions[i] - notePositions[i - 1]) * GetGenerationSecondsPerBeat();

            if (interval < minimumInterval)
            {
                return false;
            }
        }

        return true;
    }

    private void GetNoteCountRange(int difficulty, out int minimum, out int maximum)
    {
        int[] minimumNotes = { 3, 4, 5 };
        int[] maximumNotes = { 5, 8, 11 };
        float bpmScale = Mathf.Clamp(120f / generationBpm, 0.65f, 1.15f);

        minimum = minimumNotes[difficulty];
        maximum = Mathf.Max(minimum, Mathf.RoundToInt(maximumNotes[difficulty] * bpmScale));
    }

    private static float GetMinimumInputInterval(int difficulty)
    {
        float[] minimumIntervals = { 0.16f, 0.095f, 0.055f };
        return minimumIntervals[difficulty];
    }

    private float GetGenerationSecondsPerBeat()
    {
        return 60f / generationBpm;
    }

    private static BeatData CreateBeatData(RhythmPattern pattern)
    {
        return new BeatData
        {
            subdivisions = pattern.subdivisions,
            noteTicks = (int[])pattern.ticks.Clone()
        };
    }

    private static BeatData CreateCadenceBeat(int difficulty)
    {
        if (difficulty == 0)
        {
            return CreateBeatData(new RhythmPattern(1, 1, 0));
        }

        return CreateBeatData(new RhythmPattern(2, 1, 0, 1));
    }

    private static BeatData[] CreateFallbackMeasure(int difficulty, bool isFinalMeasure)
    {
        BeatData[] beats = new BeatData[RythmManager.BeatsPerMeasure];

        for (int i = 0; i < beats.Length; i++)
        {
            RhythmPattern pattern = difficulty == 0
                ? new RhythmPattern(1, 1, 0)
                : new RhythmPattern(2, 1, 0, 1);

            beats[i] = CreateBeatData(pattern);
        }

        if (isFinalMeasure)
        {
            beats[beats.Length - 1] = CreateCadenceBeat(difficulty);
        }

        return beats;
    }

    private static MeasureData CreateMeasureData(BeatData[] beats, int index, int difficulty)
    {
        MeasureData measure = ScriptableObject.CreateInstance<MeasureData>();
        measure.difficulty = (MeasureData.EDifficulty)difficulty;
        measure.name = $"Generated Measure {index + 1}";
        measure.hideFlags = HideFlags.DontSave;
        measure.beats = beats;
        return measure;
    }

    private void ReleasePreviousMeasures()
    {
        if (GeneratedMeasures == null)
        {
            return;
        }

        for (int i = 0; i < GeneratedMeasures.Length; i++)
        {
            if (GeneratedMeasures[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(GeneratedMeasures[i]);
            }
            else
            {
                DestroyImmediate(GeneratedMeasures[i]);
            }
        }
    }

    private void FindRythmManager()
    {
        if (rythmManager == null)
        {
            rythmManager = FindAnyObjectByType<RythmManager>();
        }
    }

    private readonly struct RhythmPattern
    {
        public readonly int subdivisions;
        public readonly int weight;
        public readonly int[] ticks;

        public bool ContainsTickZero => ticks.Length > 0 && ticks[0] == 0;

        public RhythmPattern(int subdivisions, int weight, params int[] ticks)
        {
            this.subdivisions = subdivisions;
            this.weight = weight;
            this.ticks = ticks;
        }
    }
}
