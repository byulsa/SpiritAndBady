using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PatternInput : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private NoteGenerator noteGenerator;
    [FormerlySerializedAs("Generator")]
    [SerializeField] private MeasureGenerator measureGenerator;

    [Header("Selection")]
    [SerializeField] private int selectionMeasureCount = 2;
    [SerializeField] private bool beginOnStart = true;

    public event Action OnSelectionTimedOut;

    public bool IsSelecting { get; private set; }
    public int SelectionEndMeasureIndex { get; private set; } = -1;

    private bool isSubscribed;

    private void OnEnable()
    {
        FindDependencies();
        SubscribeToRythmManager();
    }

    private void Start()
    {
        if (!beginOnStart)
        {
            return;
        }

        if (!HasRequiredDependencies())
        {
            return;
        }

        rythmManager.RunOnNextMeasure(BeginSelection);
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
        if (!IsSelecting)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            AddDifficulty(0);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            AddDifficulty(1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            AddDifficulty(2);
        }
    }

    public void BeginSelection()
    {
        FindDependencies();
        SubscribeToRythmManager();

        if (!HasRequiredDependencies())
        {
            return;
        }

        if (!rythmManager.IsRunning || rythmManager.CurrentMeasureIndex < 0)
        {
            Debug.LogError("Pattern selection must begin on a running rhythm measure.");
            return;
        }

        measureGenerator.ResetWave();
        SelectionEndMeasureIndex =
            rythmManager.CurrentMeasureIndex + Mathf.Max(1, selectionMeasureCount);
        IsSelecting = true;

        Debug.Log(
            $"Pattern selection started. Deadline: measure " +
            $"{SelectionEndMeasureIndex + 1}");
    }

    private void AddDifficulty(int difficulty)
    {
        MeasureData measure = measureGenerator.MakeMeasure(difficulty);

        if (measure == null || !measureGenerator.IsWaveReady)
        {
            return;
        }

        IsSelecting = false;
        MeasureData[] wave = measureGenerator.GetWaveMeasures();

        rythmManager.RunOnNextMeasure(() => noteGenerator.WaveStart(wave));
        Debug.Log("Pattern selection complete. Wave queued for the next measure.");
    }

    private void HandleMeasureStart(int measureIndex)
    {
        if (!IsSelecting || measureIndex < SelectionEndMeasureIndex)
        {
            return;
        }

        IsSelecting = false;
        Debug.LogWarning(
            $"Pattern selection timed out with " +
            $"{measureGenerator.GeneratedCount}/4 measures.");
        OnSelectionTimedOut?.Invoke();
    }

    private bool HasRequiredDependencies()
    {
        if (rythmManager != null && noteGenerator != null && measureGenerator != null)
        {
            return true;
        }

        Debug.LogError(
            "PatternInput requires RythmManager, NoteGenerator, and MeasureGenerator.");
        return false;
    }

    private void FindDependencies()
    {
        if (rythmManager == null)
        {
            rythmManager = FindAnyObjectByType<RythmManager>();
        }

        if (noteGenerator == null)
        {
            noteGenerator = FindAnyObjectByType<NoteGenerator>();
        }

        if (measureGenerator == null)
        {
            measureGenerator = FindAnyObjectByType<MeasureGenerator>();
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
        if (selectionMeasureCount < 1)
        {
            selectionMeasureCount = 1;
        }
    }
}
