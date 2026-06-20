using UnityEngine;

public class WaveQueueUI : MonoBehaviour
{
    [SerializeField] private QueueSlot[] Slots;
    [SerializeField] private PatternInput PlayerInput;
    [SerializeField] private NoteGenerator NoteGenerator;
    void Awake()
    {
        if (PlayerInput == null)
        {
            PlayerInput = FindAnyObjectByType<PatternInput>();
        }
        if (NoteGenerator == null)
        {
            NoteGenerator = FindAnyObjectByType<NoteGenerator>();
        }
    }
    private void OnEnable()
    {
        if (PlayerInput)
        {
            PlayerInput.OnSelectionTimedOut += Clear;
            PlayerInput.OnMeasureSelected += OnMeasureSelected;
        }
        if (NoteGenerator)
        {
            NoteGenerator.OnMeasureStarted += OnWaveMeasureStarted;
        }
    }
    private void OnDisable()
    {
        if (PlayerInput)
        {
            PlayerInput.OnSelectionTimedOut -= Clear;
            PlayerInput.OnMeasureSelected -= OnMeasureSelected;
        }
        if (NoteGenerator)
        {
            NoteGenerator.OnMeasureStarted -= OnWaveMeasureStarted;
        }
    }
    private void OnMeasureSelected(int SlotIndex, int Difficulty)
    {
        if (SlotIndex < 0 || SlotIndex >= Slots.Length)
        {
            Debug.Log($"Invalid UI Slot Index : {SlotIndex}");
            return;
        }
        Slots[SlotIndex].SetSlot(Difficulty);
    }
    private void OnWaveMeasureStarted(int index)
    {
        for (int i = 0; i < Slots.Length - 1 - index; i++)
        {
            Slots[i].SetSlot(Slots[i + 1].Difficulty);
        }
        Slots[Slots.Length - 1 - index].Clear();
    }
    public void Clear()
    {
        foreach (var slot in Slots)
        {
            slot.Clear();
        }
    }
}
