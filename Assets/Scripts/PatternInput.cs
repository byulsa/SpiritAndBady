using UnityEngine;

public class PatternInput : MonoBehaviour
{
    [Header("Measure Data")]
    public MeasureData measure1;
    public MeasureData measure2;
    public MeasureData measure3;
    public MeasureData measure4;

    [Header("Generator")]
    public NoteGenerator noteGenerator;

    private MeasureData[] selectedMeasures = new MeasureData[4];
    private int currentIndex = 0;
    private bool isSelecting = true;
    private void Start()
    {
        // noteGenerator.OnWaveFinished += ResetSelection;
    }

    void Update()
    {
        if (!isSelecting)
            return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            AddMeasure(measure1);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            AddMeasure(measure2);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            AddMeasure(measure3);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            AddMeasure(measure4);
        }
    }

    private void AddMeasure(MeasureData data)
    {
        selectedMeasures[currentIndex] = data;

        Debug.Log($"Measure {currentIndex + 1} 선택");

        currentIndex++;

        if (currentIndex >= 4)
        {
            isSelecting = false;

            noteGenerator.WaveStart(selectedMeasures);
        }
    }

    public void ResetSelection()
    {
        currentIndex = 0;
        selectedMeasures = new MeasureData[4];
        isSelecting = true;
    }
}