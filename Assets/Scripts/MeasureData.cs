using UnityEngine;

[CreateAssetMenu(fileName = "MeasureData", menuName = "Rhythm/Measure Data")]
public class MeasureData : ScriptableObject
{
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
}
[System.Serializable]
public class BeatData
{
    public int subdivisions = 1;
    public int[] noteTicks;
}
