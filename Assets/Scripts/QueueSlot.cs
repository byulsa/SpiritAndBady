using UnityEngine;

public class QueueSlot : MonoBehaviour
{
    [SerializeField] private GameObject[] DifficultyUIs;
    public int Difficulty { get; private set; }
    public void SetSlot(int difficulty)
    {
        Clear();
        DifficultyUIs[difficulty].SetActive(true);
        Difficulty = difficulty;
    }
    public void Clear()
    {
        foreach (var UI in DifficultyUIs)
        {
            UI.SetActive(false);
        }
        Difficulty = -1;
    }
}
