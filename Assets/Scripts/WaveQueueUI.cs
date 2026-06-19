using UnityEngine;

public class WaveQueueUI : MonoBehaviour
{
    [SerializeField] private QueueSlot[] Slots;
    [SerializeField] private PatternInput PlayerInput;

    private void OnEnable()
    {
        PlayerInput.OnSelectionTimedOut += Clear;
        PlayerInput.OnMeasureSelected += OnMeasureSelected;
    }
    private void OnDisable()
    {
        PlayerInput.OnSelectionTimedOut -= Clear;
        PlayerInput.OnMeasureSelected -= OnMeasureSelected;
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
    public void Clear()
    {
        foreach (var slot in Slots)
        {
            slot.Clear();
        }
    }
}
