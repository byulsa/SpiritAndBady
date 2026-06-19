using UnityEngine;

public class QueueSlot : MonoBehaviour
{
    [SerializeField] private GameObject[] DifficultyUIs;
    public void SetSlot(int difficulty)
    {
        Clear();
        DifficultyUIs[difficulty].SetActive(true);
    }
    public void Clear()
    {
        foreach (var UI in DifficultyUIs)
        {
            UI.SetActive(false);
        }
    }
}
