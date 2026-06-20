using UnityEngine;
using TMPro;

public class DistanceTracker : MonoBehaviour
{
    [SerializeField] private TrainSpeedController trainSpeedController;
    [SerializeField] private PatternInput patternInput;
    [SerializeField] private TextMeshProUGUI distanceText;

    private float totalDistance = 0f;
    private bool isMeasuring = false;

    void Start()
    {
        if (patternInput != null)
            patternInput.OnSelectionStarted += () => isMeasuring = true;
    }

    void OnDestroy()
    {
        if (patternInput != null)
            patternInput.OnSelectionStarted -= () => isMeasuring = true;
    }

    private void OnMeasureSelected(int _, int __)
    {
        isMeasuring = true;
    }

    void Update()
    {
        if (!isMeasuring || Time.timeScale == 0f) return;

        totalDistance += (trainSpeedController.GetCurrentSpeed() / 3600f * 1000f) * Time.deltaTime;
        distanceText.text = $"운행 거리 : {totalDistance:F2} m";
    }
}