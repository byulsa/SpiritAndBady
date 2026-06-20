using UnityEngine;

[CreateAssetMenu(fileName = "MeasureData", menuName = "Rhythm/Measure Data")]
public class MeasureData : ScriptableObject
{
    public enum EDifficulty { Easy, Noraml, Hard }
    public EDifficulty difficulty;
    public BeatData[] beats = new BeatData[4];
    private const int BeatsPerMeasure = 4;
    private void OnValidate()
    {
        if (beats == null || beats.Length != BeatsPerMeasure)
        {
            System.Array.Resize(ref beats, BeatsPerMeasure);
        }
        for (int i = 0; i < beats.Length; i++)
        {
            if (beats[i] == null)
            {
                beats[i] = new BeatData();
            }

            if (beats[i].subdivisions <= 0)
            {
                beats[i].subdivisions = 1;
            }
        }
    }
    public int GetNotes()
    {
        int result = 0;
        foreach (BeatData beat in beats)
        {
            result += beat.noteTicks.Length;
        }
        return result;
    }
}
[System.Serializable]
public class BeatData
{
    public int subdivisions = 1;
    public int[] noteTicks;
}
