using UnityEngine;

public class WaveQueueUI : MonoBehaviour
{
    [SerializeField] private QueueSlot[] Slots;
    [SerializeField] private PatternInput PlayerInput;
    [SerializeField] private NoteGenerator NoteGenerator;
    private void OnEnable()
    {
        PlayerInput.OnSelectionTimedOut += Clear;
        PlayerInput.OnMeasureSelected += OnMeasureSelected;
        NoteGenerator.OnMeasureStarted += OnWaveMeasureStarted;
    }
    private void OnDisable()
    {
        PlayerInput.OnSelectionTimedOut -= Clear;
        PlayerInput.OnMeasureSelected -= OnMeasureSelected;
        NoteGenerator.OnMeasureStarted -= OnWaveMeasureStarted;
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
