using UnityEngine;

public class NoteGeneratorTest : MonoBehaviour
{
    [SerializeField] private RythmManager rythmManager;
    [SerializeField] private NoteGenerator noteGenerator;
    [SerializeField] private MeasureData[] testWave = new MeasureData[4];
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private KeyCode replayKey = KeyCode.Space;

    private void Start()
    {
        if (startAutomatically)
        {
            QueueTestWave();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(replayKey))
        {
            QueueTestWave();
        }
    }

    [ContextMenu("Queue Test Wave")]
    public void QueueTestWave()
    {
        if (rythmManager == null || noteGenerator == null)
        {
            Debug.LogError("Assign RythmManager and NoteGenerator to NoteGeneratorTest.");
            return;
        }

        if (testWave == null || testWave.Length != 4)
        {
            Debug.LogError("Test Wave must contain exactly four MeasureData objects.");
            return;
        }

        if (!rythmManager.IsRunning)
        {
            rythmManager.StartClock();
        }

        rythmManager.RunOnNextMeasure(() => noteGenerator.WaveStart(testWave));
        Debug.Log("Test wave queued for the next measure.");
    }
}
